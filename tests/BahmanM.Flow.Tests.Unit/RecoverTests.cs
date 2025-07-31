using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow.Tests.Unit;

public class RecoverTests
{
    [Fact]
    public async Task Recover_OnSuccessfulFlow_DoesNotExecuteAndReturnsOriginalValue()
    {
        // Arrange
        var initialFlow = Flow.Succeed(10);
        var recoverExecuted = false;
        var recoveredFlow = initialFlow.Recover(ex =>
        {
            recoverExecuted = true;
            return Flow.Succeed(0);
        });

        // Act
        var outcome = await FlowEngine.ExecuteAsync(recoveredFlow);

        // Assert
        Assert.False(recoverExecuted);
        Assert.Equal(Success(10), outcome);
    }

    [Fact]
    public async Task Recover_OnFailedFlow_ExecutesAndReturnsNewFlow()
    {
        // Arrange
        var exception = new Exception("Initial failure");
        var initialFlow = Flow.Fail<int>(exception);
        var recoveredFlow = initialFlow.Recover(ex => Flow.Succeed(20));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(recoveredFlow);

        // Assert
        Assert.Equal(Success(20), outcome);
    }

    [Fact]
    public async Task WhenRecoverFunctionThrows_ReturnsFailure()
    {
        // Arrange
        var initialException = new Exception("Initial failure");
        var recoverException = new InvalidOperationException("Recover function failed!");
        var initialFlow = Flow.Fail<int>(initialException);
        var recoveredFlow = initialFlow.Recover((Flow.Operations.Recover.Sync<int>)(_ => throw recoverException));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(recoveredFlow);

        // Assert
        Assert.Equal(Failure<int>(recoverException), outcome);
    }

    [Fact]
    public async Task AsyncRecover_OnSuccessfulFlow_DoesNotExecuteAndReturnsOriginalValue()
    {
        // Arrange
        var initialFlow = Flow.Succeed(10);
        var recoverExecuted = false;
        var recoveredFlow = initialFlow.Recover(async _ =>
        {
            recoverExecuted = true;
            await Task.Delay(10);
            return Flow.Succeed(0);
        });

        // Act
        var outcome = await FlowEngine.ExecuteAsync(recoveredFlow);

        // Assert
        Assert.False(recoverExecuted);
        Assert.Equal(Success(10), outcome);
    }

    [Fact]
    public async Task AsyncRecover_OnFailedFlow_ExecutesAndReturnsNewFlow()
    {
        // Arrange
        var exception = new Exception("Initial failure");
        var initialFlow = Flow.Fail<int>(exception);
        var recoveredFlow = initialFlow.Recover(async ex =>
        {
            await Task.Delay(10);
            return Flow.Succeed(20);
        });

        // Act
        var outcome = await FlowEngine.ExecuteAsync(recoveredFlow);

        // Assert
        Assert.Equal(Success(20), outcome);
    }

    [Fact]
    public async Task WhenAsyncRecoverFunctionThrows_ReturnsFailure()
    {
        // Arrange
        var initialException = new Exception("Initial failure");
        var recoverException = new InvalidOperationException("Async recover function failed!");
        var initialFlow = Flow.Fail<int>(initialException);
        var recoveredFlow = initialFlow.Recover((Flow.Operations.Recover.Async<int>)(_ => throw recoverException));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(recoveredFlow);

        // Assert
        Assert.Equal(Failure<int>(recoverException), outcome);
    }

    [Fact]
    public async Task CancellableAsyncRecover_OnSuccessfulFlow_DoesNotExecuteAndReturnsOriginalValue()
    {
        // Arrange
        var initialFlow = Flow.Succeed(10);
        var recoverExecuted = false;
        var recoveredFlow = initialFlow.Recover(async (ex, token) =>
        {
            recoverExecuted = true;
            await Task.Delay(10, token);
            return Flow.Succeed(0);
        });

        // Act
        var outcome = await FlowEngine.ExecuteAsync(recoveredFlow);

        // Assert
        Assert.False(recoverExecuted);
        Assert.Equal(Success(10), outcome);
    }

    [Fact]
    public async Task WhenCancellableAsyncRecoverIsCancelled_ReturnsFailure()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var options = new Execution.Options(CancellationToken: cts.Token);
        var initialException = new Exception("Initial failure");

        var recoveredFlow = Flow.Fail<int>(initialException).Recover(async (ex, token) =>
        {
            await Task.Delay(100, token);
            return Flow.Succeed(20);
        });

        // Act
        await cts.CancelAsync();
        var outcome = await FlowEngine.ExecuteAsync(recoveredFlow, options);

        // Assert
        var failure = Assert.IsType<Failure<int>>(outcome);
        Assert.IsType<TaskCanceledException>(failure.Exception);

        // Clean up
        cts.Dispose();
    }
}
