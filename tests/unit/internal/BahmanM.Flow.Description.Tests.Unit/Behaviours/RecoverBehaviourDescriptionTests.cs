using BahmanM.Flow.Description.Serialisation;

namespace BahmanM.Flow.Description.Tests.Unit.Behaviours;

public class RecoverBehaviourDescriptionTests
{
    [Theory]
    [InlineData(RecoverKind.Sync, true)]
    [InlineData(RecoverKind.Sync, false)]
    [InlineData(RecoverKind.Async, true)]
    [InlineData(RecoverKind.Async, false)]
    [InlineData(RecoverKind.CancellableAsync, true)]
    [InlineData(RecoverKind.CancellableAsync, false)]
    public async Task Retry_Description_AroundRecoverDoesNotRetrySourceCallbackOrFallback(
        RecoverKind kind,
        bool returnDescription)
    {
        // Arrange
        var sourceException = new InvalidOperationException("source failed");
        var fallbackException = new InvalidOperationException("fallback failed");
        var sourceExecutions = 0;
        var callbackExecutions = 0;
        var fallbackExecutions = 0;
        Flow.Operations.Create.Sync<int> sourceOperation = () =>
        {
            sourceExecutions++;
            throw sourceException;
        };
        Flow.Operations.Create.Sync<int> fallbackOperation = () =>
        {
            fallbackExecutions++;
            throw fallbackException;
        };
        var source = (Flow.Create(() => sourceOperation())).AsDescription();
        var fallback = returnDescription
            ? (Flow.Create(() => fallbackOperation())).AsDescription()
            : Flow.Create(() => fallbackOperation());
        var recover = CreateRecover(source, fallback, kind, _ => callbackExecutions++);
        var description = ((recover).WithRetry(3)).AsDescription();

        // Act
        var descriptionText = DescriptionFormatter.Describe(description);
        var lowered = description.Lower(FacadeBehaviourStrategyFactory.Instance);
        var countsAfterLowering = (sourceExecutions, callbackExecutions, fallbackExecutions);
        var outcome = await FlowEngine.ExecuteAsync(description);

        // Assert
        Assert.Contains($"Retry<int>", descriptionText);
        Assert.Contains($"Recover.{kind}<int>", descriptionText);
        Assert.IsAssignableFrom<Ast.INode<int>>(lowered);
        Assert.Equal((0, 0, 0), countsAfterLowering);
        Assert.Same(fallbackException, Assert.IsType<Failure<int>>(outcome).Exception);
        Assert.Equal(1, sourceExecutions);
        Assert.Equal(1, callbackExecutions);
        Assert.Equal(1, fallbackExecutions);
    }

    [Theory]
    [InlineData(RecoverKind.Sync, true)]
    [InlineData(RecoverKind.Sync, false)]
    [InlineData(RecoverKind.Async, true)]
    [InlineData(RecoverKind.Async, false)]
    [InlineData(RecoverKind.CancellableAsync, true)]
    [InlineData(RecoverKind.CancellableAsync, false)]
    public async Task Timeout_Description_AroundRecoverTimesSourceButNotGeneratedFallback(
        RecoverKind kind,
        bool returnDescription)
    {
        // Arrange
        var timeout = TimeSpan.FromMilliseconds(200);
        var sourceExecutions = 0;
        var callbackExecutions = 0;
        var fallbackExecutions = 0;
        Exception? recoveredException = null;
        var sourceStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var fallbackStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var fallbackCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFallback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Flow.Operations.Create.CancellableAsync<int> sourceOperation = async token =>
        {
            sourceExecutions++;
            sourceStarted.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return 0;
        };
        Flow.Operations.Create.Async<int> fallbackOperation = async () =>
        {
            fallbackExecutions++;
            fallbackStarted.TrySetResult();
            await releaseFallback.Task;
            fallbackCompleted.TrySetResult();
            return 42;
        };
        var source = (Flow.Create(cancellationToken => sourceOperation(cancellationToken))).AsDescription();
        var fallback = returnDescription
            ? (Flow.Create(() => fallbackOperation())).AsDescription()
            : Flow.Create(() => fallbackOperation());
        var recover = CreateRecover(source, fallback, kind, exception =>
        {
            callbackExecutions++;
            recoveredException = exception;
        });
        var description = ((recover).WithTimeout(timeout)).AsDescription();

        // Act
        var descriptionText = DescriptionFormatter.Describe(description);
        var lowered = description.Lower(FacadeBehaviourStrategyFactory.Instance);
        var countsAfterLowering = (sourceExecutions, callbackExecutions, fallbackExecutions);
        var execution = FlowEngine.ExecuteAsync(description);
        await sourceStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        await fallbackStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        await Task.Delay(timeout + timeout, TestContext.Current.CancellationToken);
        var executionRemainsPendingDuringFallback = !execution.IsCompleted;
        releaseFallback.TrySetResult();
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        await fallbackCompleted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains("Timeout<int>", descriptionText);
        Assert.Contains($"Recover.{kind}<int>", descriptionText);
        Assert.IsAssignableFrom<Ast.INode<int>>(lowered);
        Assert.Equal((0, 0, 0), countsAfterLowering);
        Assert.True(executionRemainsPendingDuringFallback);
        Assert.IsType<TimeoutException>(recoveredException);
        Assert.Equal(Outcome.Success(42), outcome);
        Assert.Equal(1, sourceExecutions);
        Assert.Equal(1, callbackExecutions);
        Assert.Equal(1, fallbackExecutions);
        Assert.True(fallbackCompleted.Task.IsCompletedSuccessfully);
    }

    private static IDescription<int> CreateRecover(
        IDescription<int> source,
        IFlow<int> fallback,
        RecoverKind kind,
        Action<Exception> onRecovery) =>
        kind switch
        {
            RecoverKind.Sync => ((source).Recover(exception =>
            {
                onRecovery(exception);
                return fallback;
            })).AsDescription(),
            RecoverKind.Async => ((source).Recover(async exception =>
            {
                onRecovery(exception);
                await Task.Yield();
                return fallback;
            })).AsDescription(),
            RecoverKind.CancellableAsync => ((source).Recover(async (exception, _) =>
                {
                    onRecovery(exception);
                    await Task.Yield();
                    return fallback;
                })).AsDescription(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

    public enum RecoverKind
    {
        Sync,
        Async,
        CancellableAsync
    }
}
