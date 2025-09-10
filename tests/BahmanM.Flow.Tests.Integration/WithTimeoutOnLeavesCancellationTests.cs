using System.Collections.Concurrent;

namespace BahmanM.Flow.Tests.Integration;

public class WithTimeoutOnLeavesCancellationTests
{
    [Fact]
    public async Task WithTimeout_OnCancellableLeaf_Cancels_OnTimeout()
    {
        // Arrange
        var sideEffects = new ConcurrentBag<string>();
        var leaf = Flow.Create<string>(async ct =>
        {
            await Task.Delay(200, ct);
            sideEffects.Add("leaf completed");
            return "done";
        });

        var timed = leaf.WithTimeout(TimeSpan.FromMilliseconds(50));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(timed);
        await Task.Delay(300); // give time for wrong (uncancelled) continuation to show up

        // Assert
        Assert.True(outcome.IsFailure());
        var ex = outcome is Failure<string> f ? f.Exception : null;
        Assert.IsType<TimeoutException>(ex);
        Assert.Empty(sideEffects);
    }

    [Fact]
    public async Task WithTimeout_OnCancellableChain_Cancels_OnTimeout()
    {
        // Arrange
        var sideEffects = new ConcurrentBag<string>();
        var flow = Flow.Succeed("start").Chain(async (string _, CancellationToken ct) =>
        {
            await Task.Delay(200, ct);
            sideEffects.Add("chain inner completed");
            return Flow.Succeed("done");
        });

        var timed = flow.WithTimeout(TimeSpan.FromMilliseconds(50));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(timed);
        await Task.Delay(300);

        // Assert
        Assert.True(outcome.IsFailure());
        var ex = outcome is Failure<string> f ? f.Exception : null;
        Assert.IsType<TimeoutException>(ex);
        Assert.Empty(sideEffects);
    }

    [Fact]
    public async Task WithTimeout_OnCancellableLeaf_OuterCancellation_IsTaskCanceled()
    {
        // Arrange
        var sideEffects = new ConcurrentBag<string>();
        using var cts = new CancellationTokenSource();
        var options = new Execution.Options(cts.Token);

        var leaf = Flow.Create<string>(async ct =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            sideEffects.Add("leaf completed");
            return "done";
        }).WithTimeout(TimeSpan.FromSeconds(5));

        // Act
        var task = FlowEngine.ExecuteAsync(leaf, options);
        await Task.Yield();
        await cts.CancelAsync();
        var outcome = await task;
        await Task.Delay(50);

        // Assert
        Assert.True(outcome.IsFailure());
        var ex = outcome is Failure<string> f ? f.Exception : null;
        Assert.IsType<TaskCanceledException>(ex);
        Assert.Empty(sideEffects);
    }
}

