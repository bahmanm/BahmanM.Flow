using BahmanM.Flow;
using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow.Tests.Unit;

public class DoOnSuccessTests
{
    [Fact]
    public async Task DoOnSuccess_WhenFlowSucceeds_CallsActionAndReturnsOriginalSuccess()
    {
        // Arrange
        var successValue = 123;
        var actionCalled = false;
        Action<int> onSuccess = val =>
        {
            Assert.Equal(successValue, val);
            actionCalled = true;
        };

        var flow = Flow.Succeed(successValue).DoOnSuccess(onSuccess);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.True(actionCalled);
        Assert.Equal(Success(successValue), outcome);
    }

    [Fact]
    public async Task DoOnSuccess_WhenFlowFails_DoesNotCallActionAndReturnsOriginalFailure()
    {
        // Arrange
        var exception = new InvalidOperationException("Test Failure");
        var actionCalled = false;
        Action<int> onSuccess = _ => actionCalled = true;

        var flow = Flow.Fail<int>(exception).DoOnSuccess(onSuccess);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.False(actionCalled);
        Assert.Equal(Failure<int>(exception), outcome);
    }

    [Fact]
    public async Task DoOnSuccess_WhenActionThrows_ReturnsFailure()
    {
        // Arrange
        var successValue = 123;
        var exception = new InvalidOperationException("Action failed!");
        Action<int> onSuccess = _ => throw exception;

        var flow = Flow.Succeed(successValue).DoOnSuccess(onSuccess);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Failure<int>(exception), outcome);
    }
    
    [Fact]
    public async Task AsyncDoOnSuccess_WhenFlowSucceeds_CallsActionAndReturnsOriginalSuccess()
    {
        // Arrange
        var successValue = 123;
        var actionCalled = false;
        Func<int, Task> onSuccess = async val =>
        {
            Assert.Equal(successValue, val);
            await Task.Delay(10);
            actionCalled = true;
        };

        var flow = Flow.Succeed(successValue).DoOnSuccess(onSuccess);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.True(actionCalled);
        Assert.Equal(Success(successValue), outcome);
    }

    [Fact]
    public async Task AsyncDoOnSuccess_WhenFlowFails_DoesNotCallActionAndReturnsOriginalFailure()
    {
        // Arrange
        var exception = new InvalidOperationException("Test Failure");
        var actionCalled = false;
        Func<int, Task> onSuccess = async _ =>
        {
            await Task.Delay(10);
            actionCalled = true;
        };

        var flow = Flow.Fail<int>(exception).DoOnSuccess(onSuccess);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.False(actionCalled);
        Assert.Equal(Failure<int>(exception), outcome);
    }

    [Fact]
    public async Task AsyncDoOnSuccess_WhenActionThrows_ReturnsFailure()
    {
        // Arrange
        var successValue = 123;
        var exception = new InvalidOperationException("Action failed!");
        Func<int, Task> onSuccess = _ => throw exception;

        var flow = Flow.Succeed(successValue).DoOnSuccess(onSuccess);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Failure<int>(exception), outcome);
    }
}
