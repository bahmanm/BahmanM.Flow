using BahmanM.Flow.Execution;
using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow.Tests.Unit;

public class DoOnSuccessTests
{
    [Fact]
    public async Task WhenFlowSucceeds_CallsActionAndReturnsOriginalSuccess()
    {
        // Arrange
        var successValue = 123;
        var actionCalled = false;
        Operations.DoOnSuccess.Sync<int> onSuccess = val =>
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
    public async Task WhenFlowFails_DoesNotCallActionAndReturnsOriginalFailure()
    {
        // Arrange
        var exception = new InvalidOperationException("Test Failure");
        var actionCalled = false;
        Operations.DoOnSuccess.Sync<int> onSuccess = _ => actionCalled = true;

        var flow = Flow.Fail<int>(exception).DoOnSuccess(onSuccess);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.False(actionCalled);
        Assert.Equal(Failure<int>(exception), outcome);
    }

    [Fact]
    public async Task WhenActionThrows_ReturnsFailure()
    {
        // Arrange
        var successValue = 123;
        var exception = new InvalidOperationException("Action failed!");
        Operations.DoOnSuccess.Sync<int> onSuccess = _ => throw exception;

        var flow = Flow.Succeed(successValue).DoOnSuccess(onSuccess);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Failure<int>(exception), outcome);
    }
    
    [Fact]
    public async Task WhenFlowSucceeds_CallsAsyncActionAndReturnsOriginalSuccess()
    {
        // Arrange
        var successValue = 123;
        var actionCalled = false;
        Operations.DoOnSuccess.Async<int> onSuccess = async val =>
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
    public async Task WhenFlowFails_DoesNotCallAsyncActionAndReturnsOriginalFailure()
    {
        // Arrange
        var exception = new InvalidOperationException("Test Failure");
        var actionCalled = false;
        Operations.DoOnSuccess.Async<int> onSuccess = async _ =>
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
    public async Task WhenAsyncActionThrows_ReturnsFailure()
    {
        // Arrange
        var successValue = 123;
        var exception = new InvalidOperationException("Action failed!");
        Operations.DoOnSuccess.Async<int> onSuccess = _ => throw exception;

        var flow = Flow.Succeed(successValue).DoOnSuccess(onSuccess);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Failure<int>(exception), outcome);
    }

    [Fact]
    public async Task WhenFlowSucceeds_CallsCancellableAsyncActionAndReturnsOriginalSuccess()
    {
        // Arrange
        var successValue = 123;
        var actionCalled = false;
        Operations.DoOnSuccess.CancellableAsync<int> onSuccess = async (val, token) =>
        {
            Assert.Equal(successValue, val);
            await Task.Delay(10, token);
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
    public async Task WhenFlowFails_DoesNotCallCancellableAsyncActionAndReturnsOriginalFailure()
    {
        // Arrange
        var exception = new InvalidOperationException("Test Failure");
        var actionCalled = false;
        Operations.DoOnSuccess.CancellableAsync<int> onSuccess = async (_, _) =>
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
    public async Task WhenCancellableActionIsCancelled_ReturnsFailure()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var options = new FlowExecutionOptions { CancellationToken = cts.Token };

        Operations.DoOnSuccess.CancellableAsync<int> onSuccess = async (val, token) =>
        {
            await Task.Delay(100, token);
        };

        var flow = Flow.Succeed(123).DoOnSuccess(onSuccess);

        // Act
        await cts.CancelAsync();
        var outcome = await FlowEngine.ExecuteAsync(flow, options);

        // Assert
        var failure = Assert.IsType<Failure<int>>(outcome);
        Assert.IsType<TaskCanceledException>(failure.Exception);
    }
}
