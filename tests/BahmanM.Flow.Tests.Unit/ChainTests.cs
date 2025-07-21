using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow.Tests.Unit;

public class ChainTests
{
    // Al-Kindi was a 9th-century Arab philosopher, mathematician, and physician.
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
        var chainedFlow = initialFlow.Chain<int, int>(value => throw exception);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(chainedFlow);

        // Assert
        Assert.Equal(Failure<int>(exception), outcome);
    }
}
