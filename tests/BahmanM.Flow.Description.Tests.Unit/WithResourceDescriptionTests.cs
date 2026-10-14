using BahmanM.Flow.Description.Serialisation;
using BahmanM.Flow.Description.Tests.Unit.Support;

namespace BahmanM.Flow.Description.Tests.Unit;

public class WithResourceDescriptionTests
{
    [Fact]
    public async Task WithResource_Sync_Description_Defers_Acquire_And_Use_And_Disposes_Resource()
    {
        var acquireCalls = 0;
        var useCalls = 0;
        var childCalls = 0;
        var resource = new SyncResource();
        var description = DescriptionFlowBuilder.WithResource(
            () =>
            {
                acquireCalls++;
                return resource;
            },
            acquired =>
            {
                useCalls++;
                return DescriptionFlowBuilder.Create(() =>
                {
                    childCalls++;
                    return acquired.Value;
                });
            });
        var expected = Flow.WithResource(
            () => resource,
            acquired => Flow.Create(() => acquired.Value));

        var text = DescriptionFormatter.Describe(description);
        Assert.IsType<Ast.Resource.WithResource<SyncResource, int>>(description.Lower());
        Assert.Contains("WithResource.Sync<SyncResource, int>", text);
        Assert.Contains("acquire: lambda() -> SyncResource", text);
        Assert.Contains("use: lambda(SyncResource) -> IFlow<int>", text);
        Assert.DoesNotContain("Create.Sync<int>", text);
        Assert.Equal((0, 0, 0), (acquireCalls, useCalls, childCalls));

        var actualOutcome = await FlowEngine.ExecuteAsync(description);
        var expectedOutcome = await FlowEngine.ExecuteAsync(expected);

        Assert.Equal(expectedOutcome, actualOutcome);
        Assert.Equal(Outcome.Success(37), actualOutcome);
        Assert.Equal((1, 1, 1), (acquireCalls, useCalls, childCalls));
        Assert.True(resource.IsDisposed);
    }

    [Fact]
    public async Task WithResource_Sync_Description_Preserves_Acquire_And_Use_Failures_And_Disposal()
    {
        var acquireException = new InvalidOperationException("acquire");
        var failedAcquire = DescriptionFlowBuilder.WithResource<SyncResource, int>(
            () => throw acquireException,
            _ => throw new Xunit.Sdk.XunitException("Use must not run after acquisition fails."));
        var acquireOutcome = await FlowEngine.ExecuteAsync(failedAcquire);

        Assert.Same(acquireException, Assert.IsType<Failure<int>>(acquireOutcome).Exception);

        var useException = new InvalidOperationException("use");
        var resource = new SyncResource();
        var failedUse = DescriptionFlowBuilder.WithResource<SyncResource, int>(
            () => resource,
            _ => throw useException);
        var useOutcome = await FlowEngine.ExecuteAsync(failedUse);

        Assert.Same(useException, Assert.IsType<Failure<int>>(useOutcome).Exception);
        Assert.True(resource.IsDisposed);
    }

    [Fact]
    public async Task WithResource_Sync_Description_Disposal_Failure_Overrides_Use_Failure()
    {
        var useException = new InvalidOperationException("use");
        var disposeException = new ApplicationException("dispose");
        var resource = new SyncResource(() => throw disposeException);
        var description = DescriptionFlowBuilder.WithResource<SyncResource, int>(
            () => resource,
            _ => throw useException);

        var outcome = await FlowEngine.ExecuteAsync(description);

        Assert.Same(disposeException, Assert.IsType<Failure<int>>(outcome).Exception);
        Assert.True(resource.IsDisposed);
    }

    [Fact]
    public async Task WithResource_Async_Description_Lowers_Description_Child_And_Disposes_Asynchronously()
    {
        var acquireCalls = 0;
        var useCalls = 0;
        var resource = new AsyncResource();
        var description = DescriptionFlowBuilder.WithResource(
            async () =>
            {
                acquireCalls++;
                await Task.Yield();
                return resource;
            },
            acquired =>
            {
                useCalls++;
                return DescriptionFlowBuilder.Succeed(acquired.Value);
            });
        var expected = Flow.WithResource(
            () => Task.FromResult(resource),
            acquired => Flow.Succeed(acquired.Value));

        var text = DescriptionFormatter.Describe(description);
        var lowered = Assert.IsType<Ast.Resource.WithResourceAsync<AsyncResource, int>>(description.Lower());
        var beforeExecution = (acquireCalls, useCalls, resource.IsDisposed);
        var outcome = await FlowEngine.ExecuteAsync(description);
        var expectedOutcome = await FlowEngine.ExecuteAsync(expected);

        Assert.Contains("WithResource.Async<AsyncResource, int>", text);
        Assert.Contains("acquire: lambda() -> Task<AsyncResource>", text);
        Assert.Contains("use: lambda(AsyncResource) -> IFlow<int>", text);
        Assert.DoesNotContain("Succeed<int>", text);
        Assert.Equal((0, 0, false), beforeExecution);
        Assert.IsType<Ast.Resource.WithResourceAsync<AsyncResource, int>>(lowered);
        Assert.Equal(expectedOutcome, outcome);
        Assert.Equal(Outcome.Success(73), outcome);
        Assert.Equal((1, 1, true), (acquireCalls, useCalls, resource.IsDisposed));
    }

    [Fact]
    public async Task WithResource_CancellableAsync_Description_Passes_Token_And_Disposes_Resource()
    {
        using var cancellation = new CancellationTokenSource();
        var receivedToken = CancellationToken.None;
        var useCalls = 0;
        var resource = new AsyncResource();
        var expectedResource = new AsyncResource();
        var description = DescriptionFlowBuilder.WithResource<AsyncResource, int>(
            token =>
            {
                receivedToken = token;
                return Task.FromResult(resource);
            },
            acquired =>
            {
                useCalls++;
                return DescriptionFlowBuilder.Succeed(acquired.Value);
            });
        var expected = Flow.WithResource<AsyncResource, int>(
            token =>
            {
                Assert.Equal(cancellation.Token, token);
                return Task.FromResult(expectedResource);
            },
            acquired => Flow.Succeed(acquired.Value));

        var lowered = Assert.IsType<Ast.Resource.WithResourceCancellableAsync<AsyncResource, int>>(description.Lower());
        var outcome = await FlowEngine.ExecuteAsync(description, new Execution.Options(cancellation.Token));
        var expectedOutcome = await FlowEngine.ExecuteAsync(expected, new Execution.Options(cancellation.Token));

        Assert.IsType<Ast.Resource.WithResourceCancellableAsync<AsyncResource, int>>(lowered);
        Assert.Equal(cancellation.Token, receivedToken);
        Assert.Equal(1, useCalls);
        Assert.Equal(expectedOutcome, outcome);
        Assert.Equal(Outcome.Success(73), outcome);
        Assert.True(resource.IsDisposed);
        Assert.True(expectedResource.IsDisposed);
    }

    [Fact]
    public async Task WithResource_CancellableAsync_Description_Disposes_After_Cancelled_Use()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var resource = new AsyncResource();
        var description = DescriptionFlowBuilder.WithResource<AsyncResource, int>(
            _ => Task.FromResult(resource),
            _ => DescriptionFlowBuilder.Create(token => Task.FromCanceled<int>(token)));

        var outcome = await FlowEngine.ExecuteAsync(description, new Execution.Options(cancellation.Token));
        await resource.Disposed.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.IsAssignableFrom<OperationCanceledException>(Assert.IsType<Failure<int>>(outcome).Exception);
        Assert.True(resource.IsDisposed);
    }

    [Fact]
    public async Task WithResource_Async_Description_Disposal_Failure_Overrides_Use_Failure()
    {
        var useException = new InvalidOperationException("use");
        var disposeException = new ApplicationException("dispose");
        var resource = new AsyncResource(() => throw disposeException);
        var description = DescriptionFlowBuilder.WithResource<AsyncResource, int>(
            () => Task.FromResult(resource),
            _ => throw useException);

        var outcome = await FlowEngine.ExecuteAsync(description);

        Assert.Same(disposeException, Assert.IsType<Failure<int>>(outcome).Exception);
        Assert.True(resource.IsDisposed);
    }

    private sealed class SyncResource(Action? dispose = null) : IDisposable
    {
        public int Value => 37;
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
            dispose?.Invoke();
        }
    }

    private sealed class AsyncResource(Action? dispose = null) : IAsyncDisposable
    {
        private readonly TaskCompletionSource _disposed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int Value => 73;
        public bool IsDisposed { get; private set; }
        public Task Disposed => _disposed.Task;

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            _disposed.TrySetResult();
            dispose?.Invoke();
            return ValueTask.CompletedTask;
        }
    }
}
