using System.Collections.Concurrent;
using BahmanM.Flow.Tests.Support;
using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow.Tests.Integration;

[Collection("WithResourceDisposal")]
public class WithResourceDisposalTests
{

    [Fact]
    public async Task WithResource_Success_DisposesResource_AndYieldsValue()
    {
        // Arrange
        var expected = 42;
        var allocatedProbes = new ConcurrentBag<DisposableProbe>();
        var flow = Flow.WithResource(
            acquire: () => { var probe = new DisposableProbe(); allocatedProbes.Add(probe); return probe; },
            use: probe => Flow.Succeed(expected)
        );

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Success(expected), outcome);
        Assert.All(allocatedProbes, probe => Assert.True(probe.IsDisposed));
    }

    [Fact]
    public async Task WithResource_InnerFlowFails_DisposesResource_AndYieldsFailure()
    {
        // Arrange
        var ex = new InvalidOperationException("boom");
        var allocatedProbes = new ConcurrentBag<DisposableProbe>();
        var flow = Flow.WithResource(
            acquire: () => { var probe = new DisposableProbe(); allocatedProbes.Add(probe); return probe; },
            use: _ => Flow.Fail<int>(ex)
        );

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Failure<int>(ex), outcome);
        Assert.All(allocatedProbes, probe => Assert.True(probe.IsDisposed));
    }

    [Fact]
    public async Task WithResource_AcquireThrows_YieldsFailure_DoesNotDispose()
    {
        // Arrange
        var ex = new InvalidOperationException("acquire failed");
        var allocatedProbes = new ConcurrentBag<DisposableProbe>();
        var flow = Flow.WithResource<DisposableProbe, int>(
            acquire: () => throw ex,
            use: _ => Flow.Succeed(1)
        );

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Failure<int>(ex), outcome);
        Assert.True(allocatedProbes.IsEmpty);
    }

    [Fact]
    public async Task WithResource_UseThrows_BeforeFlowConstruction_DisposesResource_AndYieldsFailure()
    {
        // Arrange
        var ex = new InvalidOperationException("use ctor failed");
        var allocatedProbes = new ConcurrentBag<DisposableProbe>();
        var flow = Flow.WithResource<DisposableProbe, int>(
            acquire: () => { var probe = new DisposableProbe(); allocatedProbes.Add(probe); return probe; },
            use: _ => throw ex
        );

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Failure<int>(ex), outcome);
        Assert.All(allocatedProbes, probe => Assert.True(probe.IsDisposed));
    }

    [Fact]
    public async Task WithResource_InnerFlowCancelled_DisposesResource_AndYieldsTaskCanceled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var options = new Execution.Options(CancellationToken: cts.Token);

        var allocatedProbes = new ConcurrentBag<DisposableProbe>();
        var flow = Flow.WithResource(
            acquire: () => { var probe = new DisposableProbe(); allocatedProbes.Add(probe); return probe; },
            use: _ => Flow.Create<int>(async () =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cts.Token);
                return 1;
            })
        );

        // Act
        await cts.CancelAsync();
        var outcome = await FlowEngine.ExecuteAsync(flow, options);

        // Assert
        var failure = Assert.IsType<Failure<int>>(outcome);
        Assert.IsType<TaskCanceledException>(failure.Exception);
        Assert.All(allocatedProbes, probe => Assert.True(probe.IsDisposed));
    }

    [Fact]
    public async Task WithResource_InsideChain_ComposesAndDisposes()
    {
        // Arrange
        var allocatedProbes = new ConcurrentBag<DisposableProbe>();
        var flow = Flow.Succeed("ok").Chain(_ =>
            Flow.WithResource(
                acquire: () => { var probe = new DisposableProbe(); allocatedProbes.Add(probe); return probe; },
                use: _ => Flow.Succeed(123))
        );

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Success(123), outcome);
        Assert.All(allocatedProbes, probe => Assert.True(probe.IsDisposed));
    }

    [Fact]
    public async Task WithResource_Nested_DisposesInnerThenOuter()
    {
        // Arrange
        var allocatedProbes = new ConcurrentBag<DisposableProbe>();
        var flow = Flow.WithResource<DisposableProbe, int>(
            acquire: () => { var probe = new DisposableProbe(); allocatedProbes.Add(probe); return probe; },
            use: outer =>
                Flow.WithResource<DisposableProbe, int>(
                    acquire: () => { var probe = new DisposableProbe(); allocatedProbes.Add(probe); return probe; },
                    use: inner => Flow.Succeed(99)
                )
        );

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert: two allocations, two disposals
        Assert.Equal(Success(99), outcome);
        Assert.Equal(2, allocatedProbes.Count);
        Assert.All(allocatedProbes, probe => Assert.True(probe.IsDisposed));
    }

    [Fact]
    public async Task WithResource_Burst_Serial_100_DisposesAll()
    {
        await RunSerial(100, async () =>
        {
            var allocatedProbes = new ConcurrentBag<DisposableProbe>();
            var flow = Flow.WithResource<DisposableProbe, int>(
                acquire: () => { var probe = new DisposableProbe(); allocatedProbes.Add(probe); return probe; },
                use: _ => Flow.Succeed(5)
            );
            var outcome = await FlowEngine.ExecuteAsync(flow);
            Assert.Equal(Success(5), outcome);
            Assert.All(allocatedProbes, probe => Assert.True(probe.IsDisposed));
        });
    }

    [Fact]
    public async Task WithResource_Repeat_Success_10x_DisposesAll()
    {
        await RunSerial(10, async () =>
        {
            var allocatedProbes = new ConcurrentBag<DisposableProbe>();
            var flow = Flow.WithResource(
                acquire: () => { var probe = new DisposableProbe(); allocatedProbes.Add(probe); return probe; },
                use: _ => Flow.Succeed(7)
            );
            var outcome = await FlowEngine.ExecuteAsync(flow);
            Assert.Equal(Success(7), outcome);
            Assert.All(allocatedProbes, probe => Assert.True(probe.IsDisposed));
        });
    }

    [Fact]
    public async Task WithResource_Burst_Parallel_100_DisposesAll()
    {
        var exceptions = new ConcurrentBag<Exception>();
        var allocatedProbes = new ConcurrentBag<DisposableProbe>();
        await RunParallel(8, 100, async _ =>
        {
            try
            {
                var flow = Flow.WithResource(
                    acquire: () => { var probe = new DisposableProbe(); allocatedProbes.Add(probe); return probe; },
                    use: _ => Flow.Succeed(1)
                );
                var outcome = await FlowEngine.ExecuteAsync(flow);
                Assert.True(outcome.IsSuccess());
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.True(exceptions.IsEmpty, string.Join("\n", exceptions.Select(e => e.ToString())));
        Assert.All(allocatedProbes, probe => Assert.True(probe.IsDisposed));
    }

    private static async Task RunSerial(int times, Func<Task> body)
    {
        for (var i = 0; i < times; i++)
        {
            await body();
        }
    }

    private static async Task RunParallel(int concurrency, int iterations, Func<int, Task> body)
    {
        using var sem = new SemaphoreSlim(concurrency);
        var tasks = Enumerable.Range(0, iterations).Select(async i =>
        {
            await sem.WaitAsync();
            try { await body(i); }
            finally { sem.Release(); }
        });
        await Task.WhenAll(tasks);
    }
}
