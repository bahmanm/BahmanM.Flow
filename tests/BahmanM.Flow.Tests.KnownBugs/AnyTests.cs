using System.Collections.Concurrent;

namespace BahmanM.Flow.Tests.KnownBugs;

public class AnyTests
{
    [Fact(Skip = "TimeoutStrategy on Flow.Any uses WaitAsync and does not cancel underlying branches, i.e. they may still run and cause side-effects.")]
    [Trait("Category", "KnownBugs")]
    public async Task Any_WithTimeoutOnAny_ShouldCancelBranchesOnTimeout()
    {
        // Arrange
        var sideEffects = new ConcurrentBag<string>();
        var slow1 = Flow.Create<string>(async ct =>
        {
            await Task.Delay(200, ct);
            sideEffects.Add("slow1 completed");
            return "slow1";
        });

        var slow2 = Flow.Create<string>(async ct =>
        {
            await Task.Delay(220, ct);
            sideEffects.Add("slow2 completed");
            return "slow2";
        });

        var timedAny = Flow.Any(slow1, slow2).WithTimeout(TimeSpan.FromMilliseconds(50));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(timedAny);
        // Give enough time for slow branches to either be cancelled (desired) or complete (current bug)
        await Task.Delay(300);

        // Assert (desired): timeout AND no side-effects because branches were cancelled
        Assert.True(outcome.IsFailure());
        var exception = outcome switch { Failure<string> f => f.Exception, _ => null };
        Assert.IsType<TimeoutException>(exception);
        Assert.Empty(sideEffects);
    }
}
