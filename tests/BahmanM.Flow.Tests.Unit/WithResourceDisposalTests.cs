using System.Collections.Concurrent;
using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow.Tests.Unit;

[Collection("WithResourceDisposal")]
public class WithResourceDisposalTests : IDisposable
{
    public WithResourceDisposalTests() => DisposableProbe.Reset();
    public void Dispose() => DisposableProbe.Reset();

    [Fact]
    public async Task WithResource_Success_DisposesResource_AndYieldsValue()
    {
        // Arrange
        var expected = 42;
        var flow = Flow.WithResource(
            acquire: () => new DisposableProbe(),
            use: probe => Flow.Succeed(expected)
        );

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Success(expected), outcome);
        Assert.Equal(DisposableProbe.AllocatedCount, DisposableProbe.DisposedCount);
        Assert.Equal(0, DisposableProbe.FinalizedWithoutDisposeCount);
    }

    [Fact]
    public async Task WithResource_InnerFlowFails_DisposesResource_AndYieldsFailure()
    {
        // Arrange
        var ex = new InvalidOperationException("boom");
        var flow = Flow.WithResource(
            acquire: () => new DisposableProbe(),
            use: _ => Flow.Fail<int>(ex)
        );

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Failure<int>(ex), outcome);
        Assert.Equal(DisposableProbe.AllocatedCount, DisposableProbe.DisposedCount);
    }

    [Fact]
    public async Task WithResource_AcquireThrows_YieldsFailure_DoesNotDispose()
    {
        // Arrange
        var ex = new InvalidOperationException("acquire failed");
        var flow = Flow.WithResource<DisposableProbe, int>(
            acquire: () => throw ex,
            use: _ => Flow.Succeed(1)
        );

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Failure<int>(ex), outcome);
        Assert.Equal(0, DisposableProbe.AllocatedCount);
        Assert.Equal(0, DisposableProbe.DisposedCount);
    }

    [Fact]
    public async Task WithResource_UseThrows_BeforeFlowConstruction_DisposesResource_AndYieldsFailure()
    {
        // Arrange
        var ex = new InvalidOperationException("use ctor failed");
        var flow = Flow.WithResource<DisposableProbe, int>(
            acquire: () => new DisposableProbe(),
            use: _ => throw ex
        );

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Failure<int>(ex), outcome);
        Assert.Equal(DisposableProbe.AllocatedCount, DisposableProbe.DisposedCount);
    }

    [Fact]
    public async Task WithResource_InnerFlowCancelled_DisposesResource_AndYieldsTaskCanceled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var options = new Execution.Options(CancellationToken: cts.Token);

        var flow = Flow.WithResource(
            acquire: () => new DisposableProbe(),
            use: _ => Flow.Create<int>(async () =>
            {
                await Task.Delay(100, cts.Token);
                return 1;
            })
        );

        // Act
        await cts.CancelAsync();
        var outcome = await FlowEngine.ExecuteAsync(flow, options);

        // Assert
        var failure = Assert.IsType<Failure<int>>(outcome);
        Assert.IsType<TaskCanceledException>(failure.Exception);
        Assert.Equal(DisposableProbe.AllocatedCount, DisposableProbe.DisposedCount);
    }

    [Fact]
    public async Task WithResource_InsideChain_ComposesAndDisposes()
    {
        // Arrange
        var flow = Flow.Succeed("ok").Chain(_ =>
            Flow.WithResource(
                acquire: () => new DisposableProbe(),
                use: _ => Flow.Succeed(123))
        );

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Success(123), outcome);
        Assert.Equal(DisposableProbe.AllocatedCount, DisposableProbe.DisposedCount);
    }

    [Fact]
    public async Task WithResource_Nested_DisposesInnerThenOuter()
    {
        // Arrange
        DisposableProbe.Reset();
        var flow = Flow.WithResource<DisposableProbe, int>(
            acquire: () => new DisposableProbe(),
            use: outer =>
                Flow.WithResource<DisposableProbe, int>(
                    acquire: () => new DisposableProbe(),
                    use: inner => Flow.Succeed(99)
                )
        );

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert: two allocations, two disposals
        Assert.Equal(Success(99), outcome);
        Assert.Equal(2, DisposableProbe.AllocatedCount);
        Assert.Equal(2, DisposableProbe.DisposedCount);
    }

    [Fact]
    public async Task WithResource_Burst_Serial_100_DisposesAll()
    {
        await RunSerial(100, async () =>
        {
            DisposableProbe.Reset();
            var flow = Flow.WithResource<DisposableProbe, int>(
                acquire: () => new DisposableProbe(),
                use: _ => Flow.Succeed(5)
            );
            var outcome = await FlowEngine.ExecuteAsync(flow);
            Assert.Equal(Success(5), outcome);
            Assert.Equal(DisposableProbe.AllocatedCount, DisposableProbe.DisposedCount);
        });
    }

    [Fact]
    public async Task WithResource_Repeat_Success_10x_DisposesAll()
    {
        await RunSerial(10, async () =>
        {
            DisposableProbe.Reset();
            var flow = Flow.WithResource(
                acquire: () => new DisposableProbe(),
                use: _ => Flow.Succeed(7)
            );
            var outcome = await FlowEngine.ExecuteAsync(flow);
            Assert.Equal(Success(7), outcome);
            Assert.Equal(DisposableProbe.AllocatedCount, DisposableProbe.DisposedCount);
        });
    }

    [Fact]
    public async Task WithResource_Burst_Parallel_100_DisposesAll()
    {
        DisposableProbe.Reset();
        var exceptions = new ConcurrentBag<Exception>();
        await RunParallel(8, 100, async _ =>
        {
            try
            {
                var flow = Flow.WithResource(
                    acquire: () => new DisposableProbe(),
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
        Assert.Equal(DisposableProbe.AllocatedCount, DisposableProbe.DisposedCount);
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

internal sealed class DisposableProbe : IDisposable
{
    private int _disposed;
    public Guid Id { get; } = Guid.NewGuid();
    public bool IsDisposed => _disposed != 0;

    public static int AllocatedCount;
    public static int DisposedCount;
    public static int FinalizedWithoutDisposeCount;

    public DisposableProbe()
    {
        Interlocked.Increment(ref AllocatedCount);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            Interlocked.Increment(ref DisposedCount);
        }
    }

    ~DisposableProbe()
    {
        if (!IsDisposed)
        {
            Interlocked.Increment(ref FinalizedWithoutDisposeCount);
        }
    }

    public static void Reset()
    {
        AllocatedCount = 0;
        DisposedCount = 0;
        FinalizedWithoutDisposeCount = 0;
    }
}
