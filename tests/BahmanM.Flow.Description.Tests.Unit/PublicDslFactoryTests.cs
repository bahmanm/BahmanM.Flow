namespace BahmanM.Flow.Description.Tests.Unit;

public class PublicDslFactoryTests
{
    [Fact]
    public async Task Succeed_UsesDescription_AndPublicEngineExecutesIt()
    {
        var flow = Flow.Succeed("hello");

        Assert.IsType<Description.Primitive.Succeed<string>>(flow);
        Assert.Equal(new Success<string>("hello"), await FlowEngine.ExecuteAsync(flow));
    }

    [Fact]
    public async Task Fail_UsesDescription_AndPreservesExceptionIdentity()
    {
        var exception = new InvalidOperationException("failure");
        var flow = Flow.Fail<string>(exception);

        Assert.IsType<Description.Primitive.Fail<string>>(flow);
        Assert.Same(exception, Assert.IsType<Failure<string>>(await FlowEngine.ExecuteAsync(flow)).Exception);
    }

    [Fact]
    public async Task CreateSync_UsesDescription_AndDefersOperationUntilPublicExecution()
    {
        var invocationCount = 0;
        var flow = Flow.Create(() => ++invocationCount);

        Assert.IsType<Description.Create.Sync<int>>(flow);
        Assert.Equal(0, invocationCount);
        Assert.Equal(new Success<int>(1), await FlowEngine.ExecuteAsync(flow));
        Assert.Equal(1, invocationCount);
    }

    [Fact]
    public async Task CreateAsync_UsesDescription_AndDefersOperationUntilPublicExecution()
    {
        var invocationCount = 0;
        var flow = Flow.Create<int>(() => Task.FromResult(++invocationCount));

        Assert.IsType<Description.Create.Async<int>>(flow);
        Assert.Equal(0, invocationCount);
        Assert.Equal(new Success<int>(1), await FlowEngine.ExecuteAsync(flow));
        Assert.Equal(1, invocationCount);
    }

    [Fact]
    public async Task CreateCancellableAsync_UsesDescription_AndPassesPublicExecutionToken()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var observedToken = CancellationToken.None;
        var flow = Flow.Create<int>(token =>
        {
            observedToken = token;
            return Task.FromResult(1);
        });

        Assert.IsType<Description.Create.CancellableAsync<int>>(flow);
        Assert.Equal(CancellationToken.None, observedToken);
        Assert.Equal(new Success<int>(1), await FlowEngine.ExecuteAsync(flow, new(cancellationTokenSource.Token)));
        Assert.Equal(cancellationTokenSource.Token, observedToken);
    }

    [Fact]
    public async Task All_UsesDescription_PreservesChildOrder_AndExecutesThroughPublicEngine()
    {
        var first = Flow.Succeed(1);
        var second = Flow.Create(() => 2);
        var flow = Flow.All(first, second);

        var description = Assert.IsType<Description.Primitive.All<int>>(flow);
        Assert.Same(first, description.Flows[0]);
        Assert.Same(second, description.Flows[1]);
        var outcome = Assert.IsType<Success<int[]>>(await FlowEngine.ExecuteAsync(flow));
        Assert.Equal([1, 2], outcome.Value);
    }

    [Fact]
    public async Task Any_UsesDescription_PreservesChildOrder_AndExecutesThroughPublicEngine()
    {
        var first = Flow.Fail<int>(new InvalidOperationException("first branch failed"));
        var second = Flow.Succeed(2);
        var flow = Flow.Any(first, second);

        var description = Assert.IsType<Description.Primitive.Any<int>>(flow);
        Assert.Same(first, description.Flows[0]);
        Assert.Same(second, description.Flows[1]);
        Assert.Equal(new Success<int>(2), await FlowEngine.ExecuteAsync(flow));
    }

    [Fact]
    public async Task WithResource_UsesDescription_AndDefersCallbacksUntilPublicExecution()
    {
        var acquired = 0;
        var used = 0;
        var disposed = 0;
        var flow = Flow.WithResource(
            acquire: () =>
            {
                acquired++;
                return new TestResource(() => disposed++);
            },
            use: _ =>
            {
                used++;
                return Flow.Succeed("used");
            });

        Assert.IsType<Description.Resource.WithResource<TestResource, string>>(flow);
        Assert.Equal((0, 0, 0), (acquired, used, disposed));
        Assert.Equal(new Success<string>("used"), await FlowEngine.ExecuteAsync(flow));
        Assert.Equal((1, 1, 1), (acquired, used, disposed));
    }

    [Fact]
    public async Task WithResourceAsync_UsesDescription_AndDefersCallbacksUntilPublicExecution()
    {
        var acquired = 0;
        var used = 0;
        var disposed = 0;
        var flow = Flow.WithResource(
            acquireAsync: () =>
            {
                acquired++;
                return Task.FromResult(new TestAsyncResource(() => disposed++));
            },
            use: _ =>
            {
                used++;
                return Flow.Succeed("used");
            });

        Assert.IsType<Description.Resource.WithResourceAsync<TestAsyncResource, string>>(flow);
        Assert.Equal((0, 0, 0), (acquired, used, disposed));
        Assert.Equal(new Success<string>("used"), await FlowEngine.ExecuteAsync(flow));
        Assert.Equal((1, 1, 1), (acquired, used, disposed));
    }

    [Fact]
    public async Task WithResourceCancellableAsync_UsesDescription_AndPassesPublicExecutionToken()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var acquired = 0;
        var used = 0;
        var disposed = 0;
        var observedToken = CancellationToken.None;
        var flow = Flow.WithResource(
            acquireAsync: token =>
            {
                acquired++;
                observedToken = token;
                return Task.FromResult(new TestAsyncResource(() => disposed++));
            },
            use: _ =>
            {
                used++;
                return Flow.Succeed("used");
            });

        Assert.IsType<Description.Resource.WithResourceCancellableAsync<TestAsyncResource, string>>(flow);
        Assert.Equal((0, 0, 0), (acquired, used, disposed));
        Assert.Equal(new Success<string>("used"), await FlowEngine.ExecuteAsync(flow, new(cancellationTokenSource.Token)));
        Assert.Equal(cancellationTokenSource.Token, observedToken);
        Assert.Equal((1, 1, 1), (acquired, used, disposed));
    }

    private sealed class TestResource(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }

    private sealed class TestAsyncResource(Action dispose) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            dispose();
            return ValueTask.CompletedTask;
        }
    }
}
