using System.Collections.Concurrent;
using Microsoft.Extensions.Time.Testing;

namespace BahmanM.Flow.Tests.Resilience;

public class WithTimeout_over_Any_and_All_Tests
{
    private static readonly TimeSpan TimeoutDuration = TimeSpan.FromMinutes(1);

    [Fact]
    public async Task WithTimeout_OnAny_CancelsBranches_OnTimeout()
    {
        var timeProvider = new FakeTimeProvider();
        var sideEffects = new ConcurrentBag<string>();
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstToken = CancellationToken.None;
        var secondToken = CancellationToken.None;
        var slow1 = Flow.Create<string>(async ct =>
        {
            firstToken = ct;
            firstStarted.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            sideEffects.Add("slow1 completed");
            return "slow1";
        });

        var slow2 = Flow.Create<string>(async ct =>
        {
            secondToken = ct;
            secondStarted.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            sideEffects.Add("slow2 completed");
            return "slow2";
        });

        var execution = FlowEngine.ExecuteAsync(
            Flow.Any(slow1, slow2).WithTimeout(TimeoutDuration),
            CreateOptions(timeProvider));
        await Task.WhenAll(firstStarted.Task, secondStarted.Task).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        timeProvider.Advance(TimeoutDuration);
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        var failure = Assert.IsType<Failure<string>>(outcome);
        Assert.IsType<TimeoutException>(failure.Exception);
        Assert.True(firstToken.IsCancellationRequested);
        Assert.True(secondToken.IsCancellationRequested);
        Assert.Empty(sideEffects);
    }

    [Fact]
    public async Task WithTimeout_OnAll_CancelsRemaining_OnTimeout()
    {
        var timeProvider = new FakeTimeProvider();
        var sideEffects = new ConcurrentBag<string>();
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstToken = CancellationToken.None;
        var secondToken = CancellationToken.None;
        var slow1 = Flow.Create<string>(async ct =>
        {
            firstToken = ct;
            firstStarted.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            sideEffects.Add("slow1 completed");
            return "slow1";
        });

        var slow2 = Flow.Create<string>(async ct =>
        {
            secondToken = ct;
            secondStarted.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            sideEffects.Add("slow2 completed");
            return "slow2";
        });

        var execution = FlowEngine.ExecuteAsync(
            Flow.All(slow1, slow2).WithTimeout(TimeoutDuration),
            CreateOptions(timeProvider));
        await Task.WhenAll(firstStarted.Task, secondStarted.Task).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        timeProvider.Advance(TimeoutDuration);
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        var failure = Assert.IsType<Failure<string[]>>(outcome);
        Assert.IsType<TimeoutException>(failure.Exception);
        Assert.True(firstToken.IsCancellationRequested);
        Assert.True(secondToken.IsCancellationRequested);
        Assert.Empty(sideEffects);
    }

    private static Execution.Options CreateOptions(TimeProvider timeProvider) =>
        new(CancellationToken.None) { TimeProvider = timeProvider };
}
