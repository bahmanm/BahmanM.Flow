using System.Collections.Concurrent;

namespace BahmanM.Flow.Tests.Unit;

public class AnyCancellationTests
{
    [Fact]
    public async Task Any_WhenOneFlowSucceeds_CancelsOtherFlows()
    {
        // Arrange
        var sideEffects = new ConcurrentBag<string>();

        var fastFlow = Flow
            .Create<string>(async ct =>
            {
                await Task.Delay(10, ct);
                return "fast";
            });

        var slowFlow = Flow
            .Create<string>(async ct =>
            {
                try
                {
                    await Task.Delay(100, ct);
                    sideEffects.Add("slow flow ran to completion");
                    return "slow";
                }
                catch (TaskCanceledException)
                {
                    // expected cancellation
                    return "cancelled";
                }
            });

        // Act
        var outcome = await FlowEngine
            .ExecuteAsync(Flow.Any(fastFlow, slowFlow));

        // Allow time for the slow flow to observe cancellation if not already
        await Task.Delay(200);

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

        var winner = Flow
            .Create<string>(async ct =>
            {
                await Task.Delay(10, ct);
                return "win";
            });

        var slowCancellable = Flow
            .Create<string>(async ct =>
            {
                await Task.Delay(100, ct);
                cancellableSideEffects.Add("cancellable ran");
                return "cancellable";
            });

        var slowNonCancellable = Flow
            .Create<string>(async () =>
            {
                await Task.Delay(100);
                nonCancellableSideEffects.Add("non-cancellable ran");
                return "non-cancellable";
            });

        // Act
        var outcome = await FlowEngine
            .ExecuteAsync(
                Flow.Any(winner, slowCancellable, slowNonCancellable));
        await Task.Delay(200);

        // Assert
        Assert.True(outcome.IsSuccess());
        Assert.Empty(cancellableSideEffects);
        Assert.Single(nonCancellableSideEffects); // best-effort: non-cancellable may still run
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

        var slow1 = Flow
            .Create<string>(async ct =>
            {
                await Task.Delay(100, ct);
                sideEffects.Add("slow1 completed");
                return "slow1";
            });
        var slow2 = Flow
            .Create<string>(async ct =>
            {
                await Task.Delay(120, ct);
                sideEffects.Add("slow2 completed");
                return "slow2";
            });

        // Act
        var task = FlowEngine
            .ExecuteAsync(Flow.Any(slow1, slow2), options);
        await Task.Delay(20);
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
        var winner = Flow
            .Create<string>(async ct =>
            {
                await Task.Delay(10, ct);
                return "win";
            });
        var loser = Flow
            .WithResource(
                acquire: () => new DisposableProbe(),
                use: _ => Flow.Create<string>(async ct =>
                {
                    await Task.Delay(200, ct);
                    return "lose";
                })
            );

        // Act
        var outcome = await FlowEngine.ExecuteAsync(Flow.Any(winner, loser));
        await Task.Delay(200);

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
