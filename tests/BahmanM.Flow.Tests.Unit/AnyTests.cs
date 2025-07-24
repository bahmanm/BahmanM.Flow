using static BahmanM.Flow.Outcome;
using BahmanM.Flow;

namespace BahmanM.Flow.Tests.Unit;

public class AnyTests
{
    // Sophie Germain (1776-1831) was a French mathematician, physicist, and philosopher.
    // Despite initial opposition from her parents and society, she gained education from books in her
    // father's library and began corresponding with famous mathematicians.
    private const string SophieGermain = "Sophie Germain";

    [Fact]
    public async Task Any_WhenOneFlowSucceeds_ReturnsSucceededOutcomeWithFirstValue()
    {
        // Arrange
        var flow1 = Flow.Create(async () =>
        {
            await Task.Delay(20);
            return SophieGermain;
        });
        var flow2 = Flow.Fail<string>(new Exception("This one fails"));

        // Act
        var combinedFlow = Flow.Any(flow1, flow2);
        var outcome = await FlowEngine.ExecuteAsync(combinedFlow);

        // Assert
        Assert.Equal(Success(SophieGermain), outcome);
    }

    [Fact]
    public async Task Any_WhenAllFlowsFail_ReturnsFailedOutcomeWithAggregateException()
    {
        // Arrange
        var exception1 = new Exception("First failure");
        var exception2 = new Exception("Second failure");
        var flow1 = Flow.Fail<string>(exception1);
        var flow2 = Flow.Fail<string>(exception2);

        // Act
        var combinedFlow = Flow.Any(flow1, flow2);
        var outcome = await FlowEngine.ExecuteAsync(combinedFlow);

        // Assert
        Assert.True(outcome.IsFailure());
        var failure = (Failure<string>)outcome;
        var aggregateException = Assert.IsType<AggregateException>(failure.Exception);
        Assert.Equal(2, aggregateException.InnerExceptions.Count);
        Assert.Contains(exception1, aggregateException.InnerExceptions);
        Assert.Contains(exception2, aggregateException.InnerExceptions);
    }

    [Fact]
    public async Task Any_WhenMultipleFlowsSucceed_ReturnsTheFirstOneToFinish()
    {
        // Arrange
        var flow1 = Flow.Create(async () =>
        {
            await Task.Delay(50);
            return "Slow";
        });
        var flow2 = Flow.Create(async () =>
        {
            await Task.Delay(10);
            return "Fast";
        });

        // Act
        var combinedFlow = Flow.Any(flow1, flow2);
        var outcome = await FlowEngine.ExecuteAsync(combinedFlow);

        // Assert
        Assert.Equal(Success("Fast"), outcome);
    }

    [Fact]
    public async Task Any_FollowedByChain_ChainsTheFirstSuccessfulResult()
    {
        // Arrange
        var slowSuccess = Flow.Create(async () => { await Task.Delay(50); return "slow"; });
        var fastSuccess = Flow.Create(async () => { await Task.Delay(10); return "fast"; });
        var failed = Flow.Fail<string>(new Exception("failure"));

        // Act
        var combinedAndChained = Flow.Any(slowSuccess, fastSuccess, failed)
            .Chain(result => Flow.Succeed($"Chained from {result}"));

        var outcome = await FlowEngine.ExecuteAsync(combinedAndChained);

        // Assert
        Assert.Equal(Success("Chained from fast"), outcome);
    }

    [Fact]
    public async Task WithTimeout_OnAny_FailsIfNoFlowSucceedsWithinDuration()
    {
        // Arrange
        var flow1 = Flow.Create(async () => { await Task.Delay(100); return "A"; });
        var flow2 = Flow.Create(async () => { await Task.Delay(120); return "B"; });

        // Act
        var timedAny = Flow.Any(flow1, flow2).WithTimeout(TimeSpan.FromMilliseconds(50));
        var outcome = await FlowEngine.ExecuteAsync(timedAny);

        // Assert
        Assert.True(outcome.IsFailure());
        var exception = outcome switch
        {
            Failure<string> f => f.Exception,
            _ => null
        };
        Assert.IsType<TimeoutException>(exception);
    }
}
