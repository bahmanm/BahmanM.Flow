using System.Collections.Concurrent;

namespace BahmanM.Flow.Tests.Integration;

public class WithTimeoutOnCompositesCancellationTests
{
    [Fact]
    public async Task WithTimeout_OnAny_CancelsBranches_OnTimeout()
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
        // Give enough time for slow branches to either be cancelled (desired) or complete (buggy behaviour)
        await Task.Delay(300);

        // Assert (desired): timeout AND no side-effects because branches were cancelled
        Assert.True(outcome.IsFailure());
        var exception = outcome switch { Failure<string> f => f.Exception, _ => null };
        Assert.IsType<TimeoutException>(exception);
        Assert.Empty(sideEffects);
    }

    [Fact]
    public async Task WithTimeout_OnAll_CancelsRemaining_OnTimeout()
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

        var timedAll = Flow.All(slow1, slow2).WithTimeout(TimeSpan.FromMilliseconds(50));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(timedAll);
        // Allow time for wrongly-un-cancelled work to complete if it were still running
        await Task.Delay(300);

        // Assert: timeout AND no side-effects because branches were cancelled
        Assert.True(outcome.IsFailure());
        var exception = outcome switch { Failure<string[]> f => f.Exception, _ => null };
        Assert.IsType<TimeoutException>(exception);
        Assert.Empty(sideEffects);
    }
}
