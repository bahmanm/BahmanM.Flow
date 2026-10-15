using BahmanM.Flow.Description.Serialisation;
using BahmanM.Flow.Description.Tests.Unit.Support;

namespace BahmanM.Flow.Description.Tests.Unit;

public class TimeoutDescriptionTests
{
    [Fact]
    public async Task Timeout_Description_RetainsDuration_AndDefersOperationUntilExecution()
    {
        // Arrange
        var invocationCount = 0;
        Flow.Operations.Create.Sync<int> operation = () =>
        {
            invocationCount++;
            return 42;
        };
        var source = DescriptionFlowBuilder.Create(operation);
        var duration = TimeSpan.FromSeconds(5);
        var description = DescriptionFlowBuilder.Timeout(source, duration);

        // Act
        var descriptionText = DescriptionFormatter.Describe(description);
        var invocationCountAfterInspection = invocationCount;
        var lowered = description.Lower();
        var invocationCountAfterLowering = invocationCount;
        var outcome = await FlowEngine.ExecuteAsync(description);

        // Assert
        Assert.NotEqual(source.Id, description.Id);
        Assert.Contains("Timeout<int>", descriptionText);
        Assert.Contains("duration: 00:00:05", descriptionText);
        Assert.Contains("Create.Sync<int>", descriptionText);
        Assert.Equal(0, invocationCountAfterInspection);
        Assert.Equal(0, invocationCountAfterLowering);
        Assert.IsType<Ast.Create.CancellableAsync<int>>(lowered);
        Assert.Equal(42, Assert.IsType<Success<int>>(outcome).Value);
        Assert.Equal(1, invocationCount);
    }

    [Fact]
    public async Task Timeout_AsyncDescription_DefersOperation_AndTimesOutDuringExecution()
    {
        // Arrange
        var invocationCount = 0;
        var operationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var operationCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Flow.Operations.Create.Async<int> operation = async () =>
        {
            invocationCount++;
            operationStarted.TrySetResult();
            await Task.Delay(TimeSpan.FromMilliseconds(300));
            operationCompleted.TrySetResult();
            return 42;
        };
        var description = DescriptionFlowBuilder.Timeout(
            DescriptionFlowBuilder.Create(operation),
            TimeSpan.FromMilliseconds(100));

        // Act
        var descriptionText = DescriptionFormatter.Describe(description);
        var lowered = description.Lower();
        var invocationCountBeforeExecution = invocationCount;
        var execution = FlowEngine.ExecuteAsync(description);
        await operationStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var outcome = await execution;
        await operationCompleted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        // Assert
        Assert.Contains("Timeout<int>", descriptionText);
        Assert.Contains("Create.Async<int>", descriptionText);
        Assert.IsType<Ast.Create.CancellableAsync<int>>(lowered);
        Assert.Equal(0, invocationCountBeforeExecution);
        Assert.Equal(1, invocationCount);
        Assert.True(operationCompleted.Task.IsCompletedSuccessfully);
        Assert.IsType<TimeoutException>(Assert.IsType<Failure<int>>(outcome).Exception);
    }

    [Fact]
    public async Task Timeout_CancellableAsyncDescription_LinksExternalCancellationToOperationToken()
    {
        // Arrange
        using var externalCancellation = new CancellationTokenSource();
        var operationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var operationToken = CancellationToken.None;
        Flow.Operations.Create.CancellableAsync<int> operation = async token =>
        {
            operationToken = token;
            operationStarted.TrySetResult();
            await Task.Delay(global::System.Threading.Timeout.InfiniteTimeSpan, token);
            return 42;
        };
        var description = DescriptionFlowBuilder.Timeout(
            DescriptionFlowBuilder.Create(operation),
            TimeSpan.FromSeconds(5));

        // Act
        var execution = FlowEngine.ExecuteAsync(
            description,
            new Execution.Options(externalCancellation.Token));
        await operationStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await externalCancellation.CancelAsync();
        var outcome = await execution;

        // Assert
        Assert.NotEqual(externalCancellation.Token, operationToken);
        Assert.True(operationToken.IsCancellationRequested);
        Assert.IsAssignableFrom<OperationCanceledException>(Assert.IsType<Failure<int>>(outcome).Exception);
    }

    [Fact]
    public async Task Timeout_CancellableAsyncDescription_CancelsOperationTokenOnTimeout()
    {
        // Arrange
        var operationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var operationToken = CancellationToken.None;
        Flow.Operations.Create.CancellableAsync<int> operation = async token =>
        {
            operationToken = token;
            operationStarted.TrySetResult();
            await Task.Delay(global::System.Threading.Timeout.InfiniteTimeSpan, token);
            return 42;
        };
        var description = DescriptionFlowBuilder.Timeout(
            DescriptionFlowBuilder.Create(operation),
            TimeSpan.FromMilliseconds(100));

        // Act
        var execution = FlowEngine.ExecuteAsync(description);
        await operationStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var outcome = await execution;

        // Assert
        Assert.True(operationToken.IsCancellationRequested);
        Assert.IsType<TimeoutException>(Assert.IsType<Failure<int>>(outcome).Exception);
    }

    [Fact]
    public async Task LoweredTimeout_CanBeExecutedRepeatedly_WithIndependentTimeoutScopes()
    {
        // Arrange
        var invocationCount = 0;
        Flow.Operations.Create.Sync<int> operation = () => ++invocationCount;
        var description = DescriptionFlowBuilder.Timeout(
            DescriptionFlowBuilder.Create(operation),
            TimeSpan.FromMilliseconds(50));
        var lowered = description.Lower();
        await Task.Delay(TimeSpan.FromMilliseconds(100));

        // Act
        var firstOutcome = await FlowEngine.ExecuteAsync(lowered);
        var secondOutcome = await FlowEngine.ExecuteAsync(lowered);

        // Assert
        Assert.Equal(1, Assert.IsType<Success<int>>(firstOutcome).Value);
        Assert.Equal(2, Assert.IsType<Success<int>>(secondOutcome).Value);
        Assert.Equal(2, invocationCount);
    }

    [Fact]
    public async Task Timeout_AroundRetry_BoundsTheWholeRetrySequence()
    {
        // Arrange
        var attempts = 0;
        var secondAttemptStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Flow.Operations.Create.Sync<int> operation = () =>
        {
            attempts++;
            if (attempts == 1)
                throw new InvalidOperationException("try again");

            secondAttemptStarted.TrySetResult();
            Thread.Sleep(TimeSpan.FromMilliseconds(500));
            return 42;
        };
        var source = DescriptionFlowBuilder.Create(operation);
        var retried = DescriptionFlowBuilder.Retry(source, 3);
        var description = DescriptionFlowBuilder.Timeout(retried, TimeSpan.FromMilliseconds(100));

        // Act
        var execution = FlowEngine.ExecuteAsync(description);
        await secondAttemptStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var outcome = await execution;

        // Assert
        Assert.Equal(2, attempts);
        Assert.IsType<TimeoutException>(Assert.IsType<Failure<int>>(outcome).Exception);
    }

    [Fact]
    public async Task Retry_AroundTimeout_DoesNotRetryTimeoutExceptionByDefault()
    {
        // Arrange
        var attempts = 0;
        var operationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Flow.Operations.Create.Sync<int> operation = () =>
        {
            attempts++;
            operationStarted.TrySetResult();
            Thread.Sleep(TimeSpan.FromMilliseconds(500));
            return 42;
        };
        var source = DescriptionFlowBuilder.Create(operation);
        var timed = DescriptionFlowBuilder.Timeout(source, TimeSpan.FromMilliseconds(100));
        var description = DescriptionFlowBuilder.Retry(timed, 3);

        // Act
        var execution = FlowEngine.ExecuteAsync(description);
        await operationStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var outcome = await execution;

        // Assert
        Assert.Equal(1, attempts);
        Assert.IsType<TimeoutException>(Assert.IsType<Failure<int>>(outcome).Exception);
    }
}
