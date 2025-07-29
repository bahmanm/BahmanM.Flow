using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow.Tests.Unit;

public class ChainTests
{
    // Al-Kindi (c. 801–873) was a 9th-century Arab philosopher, mathematician, and physician,
    // often called the "father of Islamic philosophy." He made significant
    // contributions to various fields, including metaphysics, ethics, and optics.
    private const string AlKindi = "Al-Kindi";

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
            await Task.Delay(10);
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
            await Task.Delay(10);
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
        var chainedFlow = initialFlow.Chain(async (value, token) =>
        {
            await Task.Delay(10, token);
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
            await Task.Delay(100, token);
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
