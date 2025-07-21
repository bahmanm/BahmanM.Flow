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
}
