using BahmanM.Flow.Description.Serialisation;

namespace BahmanM.Flow.Description.Tests.Unit.Behaviours;

public class ResourceBehaviourDescriptionTests
{
    private static readonly TimeSpan TimeoutDuration = TimeSpan.FromMilliseconds(400);

    [Theory]
    [InlineData(ResourceKind.Sync)]
    [InlineData(ResourceKind.Async)]
    [InlineData(ResourceKind.CancellableAsync)]
    public async Task Retry_Description_AroundResourceDoesNotRetryAcquisitionUseOrChild(ResourceKind kind)
    {
        var exception = new InvalidOperationException("resource child failed");
        var descriptionCounts = new ResourceCounts();
        var expectedCounts = new ResourceCounts();
        var descriptionSource = CreateResourceDescription(kind, descriptionCounts, exception);
        var expectedSource = CreateExistingResourceFlow(kind, expectedCounts, exception);
        var description = ((descriptionSource).WithRetry(maxAttempts: 3)).AsDescription();
        var expected = expectedSource.WithRetry(maxAttempts: 3);

        var text = DescriptionFormatter.Describe(description);
        var lowered = description.Lower();

        Assert.Contains("Retry<int>", text);
        Assert.Contains($"WithResource.{kind}<", text);
        Assert.Equal(
            (0, 0, 0, 0),
            (descriptionCounts.AcquireCalls, descriptionCounts.UseCalls, descriptionCounts.ChildCalls, descriptionCounts.DisposalCalls));
        Assert.Equal(kind switch
        {
            ResourceKind.Sync => typeof(Ast.Resource.WithResource<SyncResource, int>),
            ResourceKind.Async => typeof(Ast.Resource.WithResourceAsync<AsyncResource, int>),
            ResourceKind.CancellableAsync => typeof(Ast.Resource.WithResourceCancellableAsync<AsyncResource, int>),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        }, lowered.GetType());

        var actualOutcome = await FlowEngine.ExecuteAsync(description);
        var expectedOutcome = await FlowEngine.ExecuteAsync(expected);

        Assert.Equal(expectedOutcome, actualOutcome);
        Assert.Same(exception, Assert.IsType<Failure<int>>(actualOutcome).Exception);
        Assert.Equal(
            (1, 1, 1, 1),
            (descriptionCounts.AcquireCalls, descriptionCounts.UseCalls, descriptionCounts.ChildCalls, descriptionCounts.DisposalCalls));
        Assert.Equal(
            (expectedCounts.AcquireCalls, expectedCounts.UseCalls, expectedCounts.ChildCalls, expectedCounts.DisposalCalls),
            (descriptionCounts.AcquireCalls, descriptionCounts.UseCalls, descriptionCounts.ChildCalls, descriptionCounts.DisposalCalls));
    }

    [Fact]
    public async Task Timeout_Description_AroundSyncResource_CancelsUse_AndWaitsForSynchronousDisposal()
    {
        var resource = new SyncResource(blockDisposal: true);
        var acquireCalls = 0;
        var useCalls = 0;
        var operationStarted = NewSignal();
        Flow.Operations.Create.CancellableAsync<int> operation = async token =>
        {
            operationStarted.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return 42;
        };
        var source = (Flow.WithResource(() =>
            {
                acquireCalls++;
                return resource;
            }, _ =>
            {
                useCalls++;
                return (Flow.Create(cancellationToken => operation(cancellationToken))).AsDescription();
            })).AsDescription();
        var description = ((source).WithTimeout(TimeoutDuration)).AsDescription();

        var text = DescriptionFormatter.Describe(description);
        var lowered = description.Lower();
        Assert.Equal((0, 0, false), (acquireCalls, useCalls, resource.IsDisposed));
        Assert.IsType<Ast.Create.CancellableAsync<int>>(lowered);

        var execution = FlowEngine.ExecuteAsync(description);
        await operationStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(2));
        await resource.DisposalStarted.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Contains("Timeout<int>", text);
        Assert.Contains("WithResource.Sync<SyncResource, int>", text);
        Assert.IsType<TimeoutException>(Assert.IsType<Failure<int>>(outcome).Exception);
        Assert.Equal((1, 1, false), (acquireCalls, useCalls, resource.IsDisposed));

        resource.ReleaseDisposal();
        await resource.Disposed.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.True(resource.IsDisposed);
    }

    [Fact]
    public async Task Timeout_Description_AroundAsyncResource_CancelsUse_AndDisposesInBackground()
    {
        var resource = new AsyncResource(blockDisposal: true);
        var useCalls = 0;
        var operationStarted = NewSignal();
        Flow.Operations.Create.CancellableAsync<int> operation = async token =>
        {
            operationStarted.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return 42;
        };
        var source = (Flow.WithResource<AsyncResource, int>(() => Task.FromResult(resource), _ =>
            {
                useCalls++;
                return (Flow.Create(cancellationToken => operation(cancellationToken))).AsDescription();
            })).AsDescription();
        var description = ((source).WithTimeout(TimeoutDuration)).AsDescription();

        var lowered = description.Lower();
        Assert.False(resource.IsDisposed);
        Assert.IsType<Ast.Create.CancellableAsync<int>>(lowered);

        var execution = FlowEngine.ExecuteAsync(description);
        await operationStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(2));
        await resource.DisposalStarted.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.IsType<TimeoutException>(Assert.IsType<Failure<int>>(outcome).Exception);
        Assert.Equal(1, useCalls);
        Assert.False(resource.IsDisposed);

        resource.ReleaseDisposal();
        await resource.Disposed.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.True(resource.IsDisposed);
    }

    [Fact]
    public async Task Timeout_Description_AroundCancellableAsyncResource_CancelsUse_AndDisposesInBackground()
    {
        var resource = new AsyncResource(blockDisposal: true);
        var acquireToken = CancellationToken.None;
        var useToken = CancellationToken.None;
        var operationStarted = NewSignal();
        Flow.Operations.Create.CancellableAsync<int> operation = async token =>
        {
            useToken = token;
            operationStarted.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return 42;
        };
        var source = (Flow.WithResource<AsyncResource, int>(token =>
            {
                acquireToken = token;
                return Task.FromResult(resource);
            }, _ => (Flow.Create(cancellationToken => operation(cancellationToken))).AsDescription())).AsDescription();
        var description = ((source).WithTimeout(TimeoutDuration)).AsDescription();

        var text = DescriptionFormatter.Describe(description);
        var lowered = description.Lower();
        Assert.Equal(CancellationToken.None, acquireToken);
        Assert.IsType<Ast.Create.CancellableAsync<int>>(lowered);

        var execution = FlowEngine.ExecuteAsync(description);
        await operationStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(2));
        await resource.DisposalStarted.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Contains("WithResource.CancellableAsync<AsyncResource, int>", text);
        Assert.IsType<TimeoutException>(Assert.IsType<Failure<int>>(outcome).Exception);
        Assert.True(acquireToken.CanBeCanceled);
        Assert.True(acquireToken.IsCancellationRequested);
        Assert.True(useToken.IsCancellationRequested);
        Assert.False(resource.IsDisposed);

        resource.ReleaseDisposal();
        await resource.Disposed.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.True(resource.IsDisposed);
    }

    [Fact]
    public async Task Timeout_Description_DuringAsyncResourceAcquisition_ReturnsTimeoutThenUsesAndDisposesResource()
    {
        var acquireStarted = NewSignal();
        var releaseAcquire = NewSignal();
        var resource = new AsyncResource();
        var useCalls = 0;
        var acquisitionCompleted = false;
        var source = (Flow.WithResource<AsyncResource, int>(async () =>
            {
                acquireStarted.TrySetResult();
                await releaseAcquire.Task;
                acquisitionCompleted = true;
                return resource;
            }, _ =>
            {
                useCalls++;
                return (Flow.Succeed(42)).AsDescription();
            })).AsDescription();
        var description = ((source).WithTimeout(TimeoutDuration)).AsDescription();

        var text = DescriptionFormatter.Describe(description);
        var lowered = description.Lower();
        Assert.False(acquisitionCompleted);
        Assert.Equal(0, useCalls);
        Assert.IsType<Ast.Create.CancellableAsync<int>>(lowered);

        var execution = FlowEngine.ExecuteAsync(description);
        await acquireStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Contains("Timeout<int>", text);
        Assert.IsType<TimeoutException>(Assert.IsType<Failure<int>>(outcome).Exception);
        Assert.False(acquisitionCompleted);
        Assert.Equal(0, useCalls);

        releaseAcquire.TrySetResult();
        await resource.Disposed.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.True(acquisitionCompleted);
        Assert.Equal(1, useCalls);
        Assert.True(resource.IsDisposed);
    }

    [Fact]
    public async Task Timeout_Description_DuringCancellableAsyncResourceAcquisition_CancelsWithoutUseOrDisposal()
    {
        var acquireStarted = NewSignal();
        var acquireCancelled = NewSignal();
        var resource = new AsyncResource();
        var useCalls = 0;
        CancellationToken acquireToken = default;
        var source = (Flow.WithResource<AsyncResource, int>(async token =>
            {
                acquireToken = token;
                acquireStarted.TrySetResult();
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, token);
                }
                catch (OperationCanceledException)
                {
                    acquireCancelled.TrySetResult();
                    throw;
                }

                return resource;
            }, _ =>
            {
                useCalls++;
                return (Flow.Succeed(42)).AsDescription();
            })).AsDescription();
        var description = ((source).WithTimeout(TimeoutDuration)).AsDescription();

        var lowered = description.Lower();
        Assert.Equal(0, useCalls);
        Assert.False(resource.IsDisposed);
        Assert.IsType<Ast.Create.CancellableAsync<int>>(lowered);

        var execution = FlowEngine.ExecuteAsync(description);
        await acquireStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(2));
        await acquireCancelled.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.IsType<TimeoutException>(Assert.IsType<Failure<int>>(outcome).Exception);
        Assert.True(acquireToken.IsCancellationRequested);
        Assert.Equal(0, useCalls);
        Assert.False(resource.IsDisposed);
    }

    [Fact]
    public async Task Timeout_Description_DuringSyncResourceAcquisition_WaitsForAcquisitionBeforeStartingTimer()
    {
        var acquireStarted = NewSignal();
        var releaseAcquire = NewSignal();
        var resource = new SyncResource();
        var useCalls = 0;
        var source = (Flow.WithResource(() =>
            {
                acquireStarted.TrySetResult();
                releaseAcquire.Task.GetAwaiter().GetResult();
                return resource;
            }, _ =>
            {
                useCalls++;
                return (Flow.Succeed(42)).AsDescription();
            })).AsDescription();
        var description = ((source).WithTimeout(TimeoutDuration)).AsDescription();
        var lowered = description.Lower();
        Assert.Equal(0, useCalls);

        var execution = Task.Run(() => FlowEngine.ExecuteAsync(description));
        await acquireStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await Task.Delay(TimeoutDuration + TimeoutDuration);
        Assert.False(execution.IsCompleted);

        releaseAcquire.TrySetResult();
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.IsType<Ast.Create.CancellableAsync<int>>(lowered);
        Assert.Equal(Outcome.Success(42), outcome);
        Assert.Equal(1, useCalls);
        Assert.True(resource.IsDisposed);
    }

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static IDescription<int> CreateResourceDescription(
        ResourceKind kind,
        ResourceCounts counts,
        Exception exception)
    {
        Flow.Operations.Create.Sync<int> child = () =>
        {
            counts.ChildCalls++;
            throw exception;
        };

        return kind switch
        {
            ResourceKind.Sync => (Flow.WithResource(() =>
                {
                    counts.AcquireCalls++;
                    return new SyncResource(onDispose: () => counts.DisposalCalls++);
                }, _ =>
                {
                    counts.UseCalls++;
                    return (Flow.Create(() => child())).AsDescription();
                })).AsDescription(),
            ResourceKind.Async => (Flow.WithResource<AsyncResource, int>(() =>
                {
                    counts.AcquireCalls++;
                    return Task.FromResult(new AsyncResource(onDispose: () => counts.DisposalCalls++));
                }, _ =>
                {
                    counts.UseCalls++;
                    return (Flow.Create(() => child())).AsDescription();
                })).AsDescription(),
            ResourceKind.CancellableAsync => (Flow.WithResource<AsyncResource, int>(_ =>
                {
                    counts.AcquireCalls++;
                    return Task.FromResult(new AsyncResource(onDispose: () => counts.DisposalCalls++));
                }, _ =>
                {
                    counts.UseCalls++;
                    return (Flow.Create(() => child())).AsDescription();
                })).AsDescription(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }

    private static IFlow<int> CreateExistingResourceFlow(
        ResourceKind kind,
        ResourceCounts counts,
        Exception exception)
    {
        Flow.Operations.Create.Sync<int> child = () =>
        {
            counts.ChildCalls++;
            throw exception;
        };

        switch (kind)
        {
            case ResourceKind.Sync:
                {
                    Func<SyncResource> acquire = () =>
                    {
                        counts.AcquireCalls++;
                        return new SyncResource(onDispose: () => counts.DisposalCalls++);
                    };
                    Func<SyncResource, IFlow<int>> use = _ =>
                    {
                        counts.UseCalls++;
                        return Flow.Create(() => child());
                    };
                    return Flow.WithResource(acquire, use);
                }
            case ResourceKind.Async:
                {
                    Func<Task<AsyncResource>> acquire = () =>
                    {
                        counts.AcquireCalls++;
                        return Task.FromResult(new AsyncResource(onDispose: () => counts.DisposalCalls++));
                    };
                    Func<AsyncResource, IFlow<int>> use = _ =>
                    {
                        counts.UseCalls++;
                        return Flow.Create(() => child());
                    };
                    return Flow.WithResource(acquire, use);
                }
            case ResourceKind.CancellableAsync:
                {
                    Func<CancellationToken, Task<AsyncResource>> acquire = _ =>
                    {
                        counts.AcquireCalls++;
                        return Task.FromResult(new AsyncResource(onDispose: () => counts.DisposalCalls++));
                    };
                    Func<AsyncResource, IFlow<int>> use = _ =>
                    {
                        counts.UseCalls++;
                        return Flow.Create(() => child());
                    };
                    return Flow.WithResource(acquire, use);
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }

    public enum ResourceKind
    {
        Sync,
        Async,
        CancellableAsync
    }

    private sealed class ResourceCounts
    {
        public int AcquireCalls { get; set; }
        public int UseCalls { get; set; }
        public int ChildCalls { get; set; }
        public int DisposalCalls { get; set; }
    }

    private sealed class SyncResource(Action? onDispose = null, bool blockDisposal = false) : IDisposable
    {
        private readonly TaskCompletionSource _disposalStarted = NewSignal();
        private readonly TaskCompletionSource _releaseDisposal = NewSignal();
        private readonly TaskCompletionSource _disposed = NewSignal();

        public bool IsDisposed { get; private set; }
        public Task DisposalStarted => _disposalStarted.Task;
        public Task Disposed => _disposed.Task;

        public void Dispose()
        {
            _disposalStarted.TrySetResult();
            if (blockDisposal)
                _releaseDisposal.Task.GetAwaiter().GetResult();

            IsDisposed = true;
            _disposed.TrySetResult();
            onDispose?.Invoke();
        }

        public void ReleaseDisposal() => _releaseDisposal.TrySetResult();
    }

    private sealed class AsyncResource(bool blockDisposal = false, Action? onDispose = null) : IAsyncDisposable
    {
        private readonly TaskCompletionSource _disposalStarted = NewSignal();
        private readonly TaskCompletionSource _releaseDisposal = NewSignal();
        private readonly TaskCompletionSource _disposed = NewSignal();

        public bool IsDisposed { get; private set; }
        public Task DisposalStarted => _disposalStarted.Task;
        public Task Disposed => _disposed.Task;

        public async ValueTask DisposeAsync()
        {
            _disposalStarted.TrySetResult();
            if (blockDisposal)
                await _releaseDisposal.Task;

            IsDisposed = true;
            _disposed.TrySetResult();
            onDispose?.Invoke();
        }

        public void ReleaseDisposal() => _releaseDisposal.TrySetResult();
    }
}
