using BahmanM.Flow.Tests.Support;
using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow.Tests.Integration;

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
        var winner = new FlowCompletionSource<string>();
        var loser = Flow.Fail<string>(new Exception("This one fails"));

        // Act
        var exec = FlowEngine.ExecuteAsync(Flow.Any(winner.Flow, loser));
        await winner.Started;
        winner.Succeed(SophieGermain);
        var outcome = await exec;

        // Assert
        Assert.Equal(Success(SophieGermain), outcome);
    }

    [Fact]
    public async Task Any_WhenAllFlowsFail_ReturnsFailedOutcomeWithAggregateException()
    {
        // Arrange
        var exception1 = new Exception("First failure");
        var exception2 = new Exception("Second failure");
        var flow1 = Flow
            .Fail<string>(exception1);
        var flow2 = Flow
            .Fail<string>(exception2);

        // Act
        var combinedFlow = Flow.Any(flow1, flow2);
        var outcome = await FlowEngine
            .ExecuteAsync(combinedFlow);

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
        var slow = new FlowCompletionSource<string>();
        var fast = new FlowCompletionSource<string>();

        var combinedFlow = Flow.Any(slow.Flow, fast.Flow);

        // Act
        var exec = FlowEngine.ExecuteAsync(combinedFlow);
        await Task.WhenAll(fast.Started, slow.Started);
        fast.Succeed("Fast");
        var outcome = await exec;
        slow.Succeed("Slow");

        // Assert
        Assert.Equal(Success("Fast"), outcome);
    }

    [Fact]
    public async Task Any_FollowedByChain_ChainsTheFirstSuccessfulResult()
    {
        // Arrange
        var slow = new FlowCompletionSource<string>();
        var fast = new FlowCompletionSource<string>();
        var failed = Flow.Fail<string>(new Exception("failure"));

        var flow = Flow
            .Any(slow.Flow, fast.Flow, failed)
            .Chain(result => Flow.Succeed($"Chained from {result}"));

        // Act
        var exec = FlowEngine.ExecuteAsync(flow);
        await Task.WhenAll(fast.Started, slow.Started);
        fast.Succeed("fast");

        var outcome = await exec;
        slow.Succeed("slow");

        // Assert
        Assert.Equal(Success("Chained from fast"), outcome);
    }

    [Fact]
    public async Task WithTimeout_OnAny_FailsIfNoFlowSucceedsWithinDuration()
    {
        // Arrange
        var never1 = Flow.Create<string>(async ct =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return "A";
        });
        var never2 = Flow.Create<string>(async ct =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return "B";
        });

        // Act
        var timedAny = Flow.Any(never1, never2).WithTimeout(TimeSpan.FromMilliseconds(50));
        var outcome = await FlowEngine.ExecuteAsync(timedAny);

        // Assert
        Assert.True(outcome.IsFailure());
        var exception = outcome switch { Failure<string> f => f.Exception, _ => null };
        Assert.IsType<TimeoutException>(exception);
    }

    [Fact]
    public async Task Any_WithTimeoutOnLeaves_DoesNotDisturbWinner()
    {
        // Arrange
        var fcs = new FlowCompletionSource<string>();
        var fast = fcs.Flow.WithTimeout(TimeSpan.FromMilliseconds(200));
        var slowTimesOut = Flow
            .Create<string>(async ct => { await Task.Delay(Timeout.InfiniteTimeSpan, ct); return "slow"; })
            .WithTimeout(TimeSpan.FromMilliseconds(50));

        // Act
        var exec = FlowEngine.ExecuteAsync(Flow.Any(fast, slowTimesOut));
        await fcs.Started;
        fcs.Succeed("fast");
        var outcome = await exec;

        // Assert
        Assert.Equal(Success("fast"), outcome);
    }

    [Fact]
    public async Task Any_FailuresPlusCancellation_AggregatesIncludingTaskCanceled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var options = new Execution.Options(cts.Token);

        var canceled = Flow.Create<string>(async ct =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return "c";
        });
        var failed = Flow.Fail<string>(new InvalidOperationException("boom"));

        // Act
        var task = FlowEngine.ExecuteAsync(Flow.Any(canceled, failed), options);
        await cts.CancelAsync();
        var outcome = await task;

        // Assert
        Assert.True(outcome.IsFailure());
        var agg = Assert.IsType<AggregateException>(((Failure<string>)outcome).Exception);
        Assert.Equal(2, agg.InnerExceptions.Count);
        Assert.Contains(agg.InnerExceptions, ex => ex is TaskCanceledException);
        Assert.Contains(agg.InnerExceptions, ex => ex is InvalidOperationException);
    }

    [Fact]
    public async Task Any_WithRetry_OnLosingBranch_DoesNotRetryAfterWinner()
    {
        // Arrange
        var attempts = 0;
        var losing = Flow
            .Create<string>(async ct =>
            {
                Interlocked.Increment(ref attempts);
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                return "loser";
            })
            .WithRetry(5);

        var winner = new FlowCompletionSource<string>();

        // Act
        var exec = FlowEngine.ExecuteAsync(Flow.Any(winner.Flow, losing));
        await winner.Started;
        winner.Succeed("winner");
        var outcome = await exec;

        // Assert
        Assert.Equal(Success("winner"), outcome);
        Assert.Equal(1, attempts);
    }
}
