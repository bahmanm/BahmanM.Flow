using System.Collections.Concurrent;
using BahmanM.Flow.Tests.Support;
using Microsoft.Extensions.Time.Testing;
using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow.Tests.Integration;

public class WithAsyncResourceDisposalTests
{
    [Fact]
    public async Task WithResource_Async_Success_DisposesResource_AndYieldsValue()
    {
        // Arrange
        var expected = 123;
        var allocated = new ConcurrentBag<AsyncDisposableProbe>();
        var flow = Flow.WithResource(
            acquireAsync: () =>
            {
                var p = new AsyncDisposableProbe();
                allocated.Add(p);
                return Task.FromResult(p);
            },
            use: _ => Flow.Succeed(expected));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Success(expected), outcome);
        Assert.All(allocated, p => Assert.True(p.IsDisposed));
    }

    [Fact]
    public async Task WithResource_Async_InnerFlowFails_DisposesResource_AndYieldsFailure()
    {
        // Arrange
        var ex = new InvalidOperationException("boom");
        var allocated = new ConcurrentBag<AsyncDisposableProbe>();
        var flow = Flow.WithResource(
            acquireAsync: () =>
            {
                var p = new AsyncDisposableProbe();
                allocated.Add(p);
                return Task.FromResult(p);
            },
            use: _ => Flow.Fail<int>(ex));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Failure<int>(ex), outcome);
        Assert.All(allocated, p => Assert.True(p.IsDisposed));
    }

    [Fact]
    public async Task WithResource_Async_AcquireThrows_YieldsFailure_DoesNotDispose()
    {
        // Arrange
        var ex = new InvalidOperationException("acquire failed");
        var allocated = new ConcurrentBag<AsyncDisposableProbe>();
        var flow = Flow.WithResource<AsyncDisposableProbe, int>(
            acquireAsync: async () => throw ex,
            use: _ => Flow.Succeed(1));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Failure<int>(ex), outcome);
        Assert.True(allocated.IsEmpty);
    }

    [Fact]
    public async Task WithResource_Async_InnerFlowCancelled_DisposesInBackground_AndYieldsTaskCanceled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var options = new Execution.Options(cts.Token);

        var probe = new AsyncDisposableProbe(delay: TimeSpan.FromMilliseconds(50));
        var flow = Flow.WithResource(
            acquireAsync: () => Task.FromResult(probe),
            use: _ => Flow.Create(async ct =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                return 1;
            }));

        // Act
        var exec = FlowEngine.ExecuteAsync(flow, options);
        await cts.CancelAsync();
        var outcome = await exec;

        // Assert outcome is cancellation
        var failure = Assert.IsType<Failure<int>>(outcome);
        Assert.IsType<TaskCanceledException>(failure.Exception);

        // And disposal happens eventually (best-effort background), possibly after outcome
        await probe.Disposed;
        Assert.True(probe.IsDisposed);
    }

    [Fact]
    public async Task WithResource_Async_Timeout_DisposesInBackground_AndYieldsTimeout()
    {
        var timeProvider = new FakeTimeProvider();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var probe = new AsyncDisposableProbe(delay: TimeSpan.FromMilliseconds(50));
        var never = Flow.WithResource(
            acquireAsync: () => Task.FromResult(probe),
            use: _ => Flow.Create(async ct =>
            {
                started.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                return 42;
            }));

        var timed = never.WithTimeout(TimeSpan.FromMinutes(1));

        var options = new Execution.Options(CancellationToken.None) { TimeProvider = timeProvider };
        var execution = FlowEngine.ExecuteAsync(timed, options);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        timeProvider.Advance(TimeSpan.FromMinutes(1));
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        var failure = Assert.IsType<Failure<int>>(outcome);
        Assert.IsType<TimeoutException>(failure.Exception);

        await probe.Disposed;
        Assert.True(probe.IsDisposed);
    }

    [Fact]
    public async Task WithResource_Async_DisposeFailure_OverridesOutcome_WhenNotCancelled()
    {
        // Arrange
        var disposeEx = new InvalidOperationException("dispose failed");
        var flow = Flow.WithResource(
            acquireAsync: () => Task.FromResult<IAsyncDisposable>(new ThrowingAsyncDisposableProbe(disposeEx)),
            use: _ => Flow.Succeed(7));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert: disposal exception overrides success
        var failure = Assert.IsType<Failure<int>>(outcome);
        Assert.Same(disposeEx, failure.Exception);
    }

    [Fact]
    public async Task WithResource_Async_Nested_DisposesBoth_OnSuccess()
    {
        // Arrange
        var outer = new AsyncDisposableProbe();
        var inner = new AsyncDisposableProbe();

        var flow = Flow.WithResource(
            acquireAsync: () => Task.FromResult(outer),
            use: _ => Flow.WithResource(
                acquireAsync: () => Task.FromResult(inner),
                use: _ => Flow.Succeed(99)));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Success(99), outcome);
        Assert.True(outer.IsDisposed);
        Assert.True(inner.IsDisposed);
    }
}
