using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow.Tests.Integration;

public class ChainTests
{
    [Fact]
    public async Task Chain_OnSuccessfulFlow_ExecutesAndReturnsNewFlow()
    {
        // Arrange
        var initialFlow = Flow.Succeed(10);
        var chainedFlow = initialFlow.Chain(value => Flow.Succeed(value * 2));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(chainedFlow);

        // Assert
        Assert.Equal(Success(20), outcome);
    }

    [Fact]
    public async Task Chain_OnFailedFlow_DoesNotExecuteAndPropagatesFailure()
    {
        // Arrange
        var exception = new Exception("Initial failure");
        var initialFlow = Flow.Fail<int>(exception);
        var chainExecuted = false;
        var chainedFlow = initialFlow.Chain(value =>
        {
            chainExecuted = true;
            return Flow.Succeed(value * 2);
        });

        // Act
        var outcome = await FlowEngine.ExecuteAsync(chainedFlow);

        // Assert
        Assert.False(chainExecuted);
        Assert.Equal(Failure<int>(exception), outcome);
    }

    [Fact]
    public async Task WhenChainFunctionThrows_ReturnsFailure()
    {
        // Arrange
        var exception = new InvalidOperationException("Chain function failed!");
        var initialFlow = Flow.Succeed(10);
        var chainedFlow = initialFlow.Chain((Flow.Operations.Chain.Sync<int, int>)((_) => throw exception));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(chainedFlow);

        // Assert
        Assert.Equal(Failure<int>(exception), outcome);
    }

    [Fact]
    public async Task AsyncChain_OnSuccessfulFlow_ExecutesAndReturnsNewFlow()
    {
        // Arrange
        var initialFlow = Flow.Succeed(10);
        var chainedFlow = initialFlow.Chain(async value =>
        {
            await Task.Yield();
            return Flow.Succeed(value * 2);
        });

        // Act
        var outcome = await FlowEngine.ExecuteAsync(chainedFlow);

        // Assert
        Assert.Equal(Success(20), outcome);
    }

    [Fact]
    public async Task AsyncChain_OnFailedFlow_DoesNotExecuteAndPropagatesFailure()
    {
        // Arrange
        var exception = new Exception("Initial failure");
        var initialFlow = Flow.Fail<int>(exception);
        var chainExecuted = false;
        var chainedFlow = initialFlow.Chain(async value =>
        {
            chainExecuted = true;
            await Task.Yield();
            return Flow.Succeed(value * 2);
        });

        // Act
        var outcome = await FlowEngine.ExecuteAsync(chainedFlow);

        // Assert
        Assert.False(chainExecuted);
        Assert.Equal(Failure<int>(exception), outcome);
    }

    [Fact]
    public async Task WhenAsyncChainFunctionThrows_ReturnsFailure()
    {
        // Arrange
        var exception = new InvalidOperationException("Async chain function failed!");
        var initialFlow = Flow.Succeed(10);
        var chainedFlow = initialFlow.Chain((Flow.Operations.Chain.Async<int, int>)((_) => throw exception));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(chainedFlow);

        // Assert
        Assert.Equal(Failure<int>(exception), outcome);
    }

    [Fact]
    public async Task CancellableAsyncChain_OnSuccessfulFlow_ExecutesAndReturnsNewFlow()
    {
        // Arrange
        var initialFlow = Flow.Succeed(10);
        var chainedFlow = initialFlow.Chain(async (value, _) =>
        {
            await Task.Yield();
            return Flow.Succeed(value * 2);
        });

        // Act
        var outcome = await FlowEngine.ExecuteAsync(chainedFlow);

        // Assert
        Assert.Equal(Success(20), outcome);
    }

    [Fact]
    public async Task WhenCancellableAsyncChainIsCancelled_ReturnsFailure()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var options = new Execution.Options(CancellationToken: cts.Token);

        var chainedFlow = Flow.Succeed(10).Chain(async (value, token) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return Flow.Succeed(value * 2);
        });

        // Act
        await cts.CancelAsync();
        var outcome = await FlowEngine.ExecuteAsync(chainedFlow, options);

        // Assert
        var failure = Assert.IsType<Failure<int>>(outcome);
        Assert.IsType<TaskCanceledException>(failure.Exception);

        // Clean up
        cts.Dispose();
    }
}
