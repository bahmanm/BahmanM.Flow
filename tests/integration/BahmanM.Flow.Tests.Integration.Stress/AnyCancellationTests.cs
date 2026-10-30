using System.Collections.Concurrent;
using BahmanM.Flow.Tests.Support;

namespace BahmanM.Flow.Tests.Integration.Stress;

public class AnyCancellationTests
{
    [Fact]
    public async Task Any_OuterCancellation_BeforeStart_DoesNotStartBranches()
    {
        // Arrange
        var sideEffects = new ConcurrentBag<string>();
        using var cts = new CancellationTokenSource();
        var options = new Execution.Options(cts.Token);

        var a = Flow
            .Create<string>(async ct =>
            {
                sideEffects.Add("a started");
                await Task.Delay(10, ct);
                return "a";
            });

        var b = Flow
            .Create<string>(async ct =>
            {
                sideEffects.Add("b started");
                await Task.Delay(10, ct);
                return "b";
            });

        // Act
        await cts.CancelAsync();
        var outcome = await FlowEngine
            .ExecuteAsync(Flow.Any(a, b), options);

        // Assert
        Assert.True(outcome.IsFailure());
        Assert.Empty(sideEffects);
        var agg = Assert.IsType<AggregateException>(((Failure<string>)outcome).Exception);
        Assert.All(agg.InnerExceptions, ex => Assert.IsType<TaskCanceledException>(ex));
    }

    [Fact]
    public async Task Any_OuterCancellation_DuringRace_CancelsAll()
    {
        // Arrange
        var sideEffects = new ConcurrentBag<string>();
        using var cts = new CancellationTokenSource();
        var options = new Execution.Options(cts.Token);

        var slow1Started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var slow1 = Flow.Create<string>(async ct =>
        {
            slow1Started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            sideEffects.Add("slow1 completed");
            return "slow1";
        });

        var slow2Started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var slow2 = Flow.Create<string>(async ct =>
        {
            slow2Started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            sideEffects.Add("slow2 completed");
            return "slow2";
        });

        // Act
        var task = FlowEngine.ExecuteAsync(Flow.Any(slow1, slow2), options);
        await Task.WhenAll(slow1Started.Task, slow2Started.Task);
        await cts.CancelAsync();
        var outcome = await task;

        // Assert
        Assert.True(outcome.IsFailure());
        var agg = Assert.IsType<AggregateException>(((Failure<string>)outcome).Exception);
        Assert.All(agg.InnerExceptions, ex => Assert.IsType<TaskCanceledException>(ex));
        Assert.Empty(sideEffects);
    }

    [Fact]
    public async Task Any_LoserWithResource_DisposesOnCancel()
    {
        // Arrange
        var winner = new FlowCompletionSource<string>();

        var probe = new DisposableProbe();
        var loserStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var loser = Flow.WithResource(
            acquire: () => probe,
            use: _ => Flow.Create<string>(async ct =>
            {
                loserStarted.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                return "lose";
            })
        );

        // Act
        var exec = FlowEngine.ExecuteAsync(Flow.Any(winner.Flow, loser));
        await Task.WhenAll(winner.Started, loserStarted.Task);
        winner.Succeed("win");
        var outcome = await exec;

        // Ensure loser observed cancellation and resource disposed
        await probe.Disposed;

        // Assert
        Assert.True(outcome.IsSuccess());
        Assert.True(probe.IsDisposed);
    }

    [Fact]
    public async Task Any_Scale_OneFastSuccess_ManyLosers_RemainsResponsive()
    {
        // Arrange
        var losers = Enumerable
            .Range(0, 50)
            .Select(_ => FlowTestHelpers.NeverCompletesUntilCancelled<string>())
            .ToArray();

        var winner = new FlowCompletionSource<string>();

        // Act
        var exec = FlowEngine.ExecuteAsync(Flow.Any(winner.Flow, losers));
        await winner.Started;
        winner.Succeed("win");
        var outcome = await exec;

        // Assert
        Assert.True(outcome.IsSuccess());
        Assert.Equal("win", outcome.GetOrElse("fallback"));
    }
}
