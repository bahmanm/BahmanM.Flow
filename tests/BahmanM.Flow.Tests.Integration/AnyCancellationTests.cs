using System.Collections.Concurrent;
using BahmanM.Flow.Tests.Support;

namespace BahmanM.Flow.Tests.Integration;

public class AnyCancellationTests
{
    [Fact]
    public async Task Any_WhenOneFlowSucceeds_CancelsOtherFlows()
    {
        // Arrange
        var sideEffects = new ConcurrentBag<string>();

        var fast = new FlowCompletionSource<string>();

        var slowStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var slowFlow = Flow.Create<string>(async ct =>
        {
            slowStarted.TrySetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                sideEffects.Add("slow flow ran to completion");
                return "slow";
            }
            catch (TaskCanceledException)
            {
                // expected cancellation
                return "cancelled";
            }
        });

        var exec = FlowEngine.ExecuteAsync(Flow.Any(fast.Flow, slowFlow));
        await Task.WhenAll(fast.Started, slowStarted.Task);
        fast.Succeed("fast");
        var outcome = await exec;

        // Assert
        Assert.True(outcome.IsSuccess());
        Assert.Empty(sideEffects);
    }

    [Fact]
    public async Task Any_FirstSuccess_WithMixedCancellableAndNonCancellable_BestEffort()
    {
        // Arrange
        var cancellableSideEffects = new ConcurrentBag<string>();
        var nonCancellableSideEffects = new ConcurrentBag<string>();

        var winner = new FlowCompletionSource<string>();

        var cancellableStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var slowCancellable = Flow.Create<string>(async ct =>
        {
            cancellableStarted.TrySetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                cancellableSideEffects.Add("cancellable ran");
                return "cancellable";
            }
            catch (TaskCanceledException)
            {
                return "cancelled";
            }
        });

        var nonCancellableDone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var slowNonCancellable = Flow.Create<string>(async () =>
        {
            await Task.Yield();
            nonCancellableSideEffects.Add("non-cancellable ran");
            nonCancellableDone.TrySetResult();
            return "non-cancellable";
        });

        // Act
        var exec = FlowEngine.ExecuteAsync(Flow.Any(winner.Flow, slowCancellable, slowNonCancellable));
        await Task.WhenAll(winner.Started, cancellableStarted.Task);
        winner.Succeed("win");
        var outcome = await exec;

        // Wait for non-cancellable branch to complete its side-effect
        await nonCancellableDone.Task;

        // Assert
        Assert.True(outcome.IsSuccess());
        Assert.Empty(cancellableSideEffects);
        Assert.Single(nonCancellableSideEffects);
    }

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
        DisposableProbe.Reset();
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
        Assert.Equal(DisposableProbe.AllocatedCount, DisposableProbe.DisposedCount);
    }

    [Fact]
    public async Task Any_Scale_OneFastSuccess_ManyLosers_RemainsResponsive()
    {
        // Arrange
        var losers = Enumerable
            .Range(0, 50)
            .Select(_ =>
                Flow.Create<string>(async ct =>
                {
                    await Task.Delay(100, ct);
                    return "slow";
                })
            )
            .ToArray();

        var winner = Flow
            .Create<string>(async ct =>
            {
                await Task.Delay(10, ct);
                return "win";
            });

        // Act
        var outcome = await FlowEngine
            .ExecuteAsync(Flow.Any(winner, losers));

        // Assert
        Assert.True(outcome.IsSuccess());
        Assert.Equal("win", outcome.GetOrElse("fallback"));
    }
}
