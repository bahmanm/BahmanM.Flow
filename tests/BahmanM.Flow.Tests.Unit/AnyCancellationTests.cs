using System.Collections.Concurrent;

namespace BahmanM.Flow.Tests.Unit;

public class AnyCancellationTests
{
    [Fact(Skip = "Awaiting robust CancellationToken support throughout the FlowEngine (see issue #56)")]
    public async Task Any_WhenOneFlowSucceeds_CancelsOtherFlows()
    {
        // Arrange
        var sideEffects = new ConcurrentBag<string>();

        var fastFlow = Flow.Create(async () =>
        {
            await Task.Delay(10);
            return "fast";
        });

        var slowFlow = Flow.Create(async () =>
        {
            await Task.Delay(100);
            sideEffects.Add("slow flow ran to completion");
            return "slow";
        });

        // Act
        var outcome = await FlowEngine.ExecuteAsync(Flow.Any(fastFlow, slowFlow));

        // Allow time for the slow flow to complete if it wasn't cancelled
        await Task.Delay(200);

        // Assert
        Assert.True(outcome.IsSuccess());
        Assert.Empty(sideEffects); // This will fail with the current implementation
    }
}
