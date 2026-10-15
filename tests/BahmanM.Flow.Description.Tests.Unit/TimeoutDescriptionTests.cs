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
