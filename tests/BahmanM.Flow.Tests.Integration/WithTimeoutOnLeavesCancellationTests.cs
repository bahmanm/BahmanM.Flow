using System.Collections.Concurrent;
using Microsoft.Extensions.Time.Testing;

namespace BahmanM.Flow.Tests.Integration;

public class WithTimeoutOnLeavesCancellationTests
{
    private static readonly TimeSpan TimeoutDuration = TimeSpan.FromMinutes(1);

    [Fact]
    public async Task WithTimeout_OnCancellableLeaf_Cancels_OnTimeout()
    {
        var timeProvider = new FakeTimeProvider();
        var sideEffects = new ConcurrentBag<string>();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var operationToken = CancellationToken.None;
        var leaf = Flow.Create<string>(async ct =>
        {
            operationToken = ct;
            started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            sideEffects.Add("leaf completed");
            return "done";
        }).WithTimeout(TimeoutDuration);

        var execution = FlowEngine.ExecuteAsync(leaf, CreateOptions(timeProvider));
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        timeProvider.Advance(TimeoutDuration);
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(5));

        var failure = Assert.IsType<Failure<string>>(outcome);
        Assert.IsType<TimeoutException>(failure.Exception);
        Assert.True(operationToken.IsCancellationRequested);
        Assert.Empty(sideEffects);
    }

    [Fact]
    public async Task WithTimeout_OnCancellableChain_Cancels_OnTimeout()
    {
        var timeProvider = new FakeTimeProvider();
        var sideEffects = new ConcurrentBag<string>();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var operationToken = CancellationToken.None;
        var flow = Flow.Succeed("start").Chain(async (string _, CancellationToken ct) =>
        {
            operationToken = ct;
            started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            sideEffects.Add("chain inner completed");
            return Flow.Succeed("done");
        }).WithTimeout(TimeoutDuration);

        var execution = FlowEngine.ExecuteAsync(flow, CreateOptions(timeProvider));
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        timeProvider.Advance(TimeoutDuration);
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(5));

        var failure = Assert.IsType<Failure<string>>(outcome);
        Assert.IsType<TimeoutException>(failure.Exception);
        Assert.True(operationToken.IsCancellationRequested);
        Assert.Empty(sideEffects);
    }

    [Fact]
    public async Task WithTimeout_OnCancellableLeaf_OuterCancellation_IsTaskCanceled()
    {
        var sideEffects = new ConcurrentBag<string>();
        using var cts = new CancellationTokenSource();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var options = new Execution.Options(cts.Token);
        var operationToken = CancellationToken.None;

        var leaf = Flow.Create<string>(async ct =>
        {
            operationToken = ct;
            started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            sideEffects.Add("leaf completed");
            return "done";
        }).WithTimeout(TimeoutDuration);

        var execution = FlowEngine.ExecuteAsync(leaf, options);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await cts.CancelAsync();
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(5));

        var failure = Assert.IsType<Failure<string>>(outcome);
        Assert.IsType<TaskCanceledException>(failure.Exception);
        Assert.True(operationToken.IsCancellationRequested);
        Assert.Empty(sideEffects);
    }

    private static Execution.Options CreateOptions(TimeProvider timeProvider) =>
        new(CancellationToken.None) { TimeProvider = timeProvider };
}
