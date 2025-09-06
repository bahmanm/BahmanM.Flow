using System.Collections.Concurrent;

namespace BahmanM.Flow.Tests.Unit;

public class AnyCancellationTests
{
    [Fact]
    public async Task Any_WhenOneFlowSucceeds_CancelsOtherFlows()
    {
        // Arrange
        var sideEffects = new ConcurrentBag<string>();

        var fastFlow = Flow.Create<string>(async ct =>
        {
            await Task.Delay(10, ct);
            return "fast";
        });

        var slowFlow = Flow.Create<string>(async ct =>
        {
            try
            {
                await Task.Delay(100, ct);
                sideEffects.Add("slow flow ran to completion");
                return "slow";
            }
            catch (TaskCanceledException)
            {
                // expected cancellation
                return "cancelled";
            }
        });

        // Act
        var outcome = await FlowEngine.ExecuteAsync(Flow.Any(fastFlow, slowFlow));

        // Allow time for the slow flow to observe cancellation if not already
        await Task.Delay(200);

        // Assert
        Assert.True(outcome.IsSuccess());
        Assert.Empty(sideEffects);
    }
}
