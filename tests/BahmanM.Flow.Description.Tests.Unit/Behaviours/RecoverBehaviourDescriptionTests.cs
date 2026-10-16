using BahmanM.Flow.Description.Serialisation;
using BahmanM.Flow.Description.Tests.Unit.Support;

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
        var source = DescriptionFlowBuilder.Create(sourceOperation);
        var fallback = returnDescription
            ? DescriptionFlowBuilder.Create(fallbackOperation)
            : Flow.Create(() => fallbackOperation());
        var recover = CreateRecover(source, fallback, kind, _ => callbackExecutions++);
        var description = DescriptionFlowBuilder.Retry(recover, 3);

        // Act
        var descriptionText = DescriptionFormatter.Describe(description);
        var lowered = description.Lower();
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
        var timeout = TimeSpan.FromMilliseconds(100);
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
        var source = DescriptionFlowBuilder.Create(sourceOperation);
        var fallback = returnDescription
            ? DescriptionFlowBuilder.Create(fallbackOperation)
            : Flow.Create(() => fallbackOperation());
        var recover = CreateRecover(source, fallback, kind, exception =>
        {
            callbackExecutions++;
            recoveredException = exception;
        });
        var description = DescriptionFlowBuilder.Timeout(recover, timeout);

        // Act
        var descriptionText = DescriptionFormatter.Describe(description);
        var lowered = description.Lower();
        var countsAfterLowering = (sourceExecutions, callbackExecutions, fallbackExecutions);
        var execution = FlowEngine.ExecuteAsync(description);
        await sourceStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await fallbackStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await Task.Delay(timeout + timeout);
        var executionRemainsPendingDuringFallback = !execution.IsCompleted;
        releaseFallback.TrySetResult();
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(2));
        await fallbackCompleted.Task.WaitAsync(TimeSpan.FromSeconds(2));

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
            RecoverKind.Sync => DescriptionFlowBuilder.Recover(source, exception =>
            {
                onRecovery(exception);
                return fallback;
            }),
            RecoverKind.Async => DescriptionFlowBuilder.Recover(source, async exception =>
            {
                onRecovery(exception);
                await Task.Yield();
                return fallback;
            }),
            RecoverKind.CancellableAsync => DescriptionFlowBuilder.Recover(
                source,
                async (exception, _) =>
                {
                    onRecovery(exception);
                    await Task.Yield();
                    return fallback;
                }),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

    public enum RecoverKind
    {
        Sync,
        Async,
        CancellableAsync
    }
}
