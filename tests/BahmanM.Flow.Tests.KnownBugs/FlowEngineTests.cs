namespace BahmanM.Flow.Tests.KnownBugs;

public class FlowEngineTests
{
    [Fact(Skip = "Recursive implementation is not stack-safe for long chains.")]
    [Trait("Category", "KnownBugs")]
    public async Task ExecuteAsync_WithLongChain_DoesNotCauseStackOverflow()
    {
        // Arrange
        const int chainLength = 10_000;
        var flow = Flow.Succeed(0);
        var counter = 0;

        for (var i = 0; i < chainLength; i++)
        {
            flow = flow.DoOnSuccess(_ => counter++);
        }

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.IsType<Success<int>>(outcome);
        Assert.Equal(chainLength, counter);
    }
}
