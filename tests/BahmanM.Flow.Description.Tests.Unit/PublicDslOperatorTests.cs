namespace BahmanM.Flow.Description.Tests.Unit;

public class PublicDslOperatorTests
{
    [Fact]
    public void OrdinaryOperatorOverloads_ProduceTheirDescriptionVariants()
    {
        var succeeded = Flow.Succeed(1);
        var failed = Flow.Fail<int>(new InvalidOperationException("failure"));

        Flow.Operations.DoOnSuccess.Sync<int> successSync = _ => { };
        Flow.Operations.DoOnSuccess.Async<int> successAsync = _ => Task.CompletedTask;
        Flow.Operations.DoOnSuccess.CancellableAsync<int> successCancellable = (_, _) => Task.CompletedTask;
        Flow.Operations.DoOnFailure.Sync failureSync = _ => { };
        Flow.Operations.DoOnFailure.Async failureAsync = _ => Task.CompletedTask;
        Flow.Operations.DoOnFailure.CancellableAsync failureCancellable = (_, _) => Task.CompletedTask;
        Flow.Operations.Select.Sync<int, string> selectSync = value => value.ToString();
        Flow.Operations.Select.Async<int, string> selectAsync = value => Task.FromResult(value.ToString());
        Flow.Operations.Select.CancellableAsync<int, string> selectCancellable =
            (value, _) => Task.FromResult(value.ToString());
        Flow.Operations.Chain.Sync<int, string> chainSync = value => Flow.Succeed(value.ToString());
        Flow.Operations.Chain.Async<int, string> chainAsync =
            value => Task.FromResult(Flow.Succeed(value.ToString()));
        Flow.Operations.Chain.CancellableAsync<int, string> chainCancellable =
            (value, _) => Task.FromResult(Flow.Succeed(value.ToString()));
        Func<int, bool> validateSync = _ => true;
        Func<int, Task<bool>> validateAsync = _ => Task.FromResult(true);
        Func<int, CancellationToken, Task<bool>> validateCancellable = (_, _) => Task.FromResult(true);

        object[] flows =
        [
            succeeded.DoOnSuccess(successSync),
            succeeded.DoOnSuccess(successAsync),
            succeeded.DoOnSuccess(successCancellable),
            failed.DoOnFailure(failureSync),
            failed.DoOnFailure(failureAsync),
            failed.DoOnFailure(failureCancellable),
            succeeded.Select(selectSync),
            succeeded.Select(selectAsync),
            succeeded.Select(selectCancellable),
            succeeded.Chain(chainSync),
            succeeded.Chain(chainAsync),
            succeeded.Chain(chainCancellable),
            succeeded.Validate(validateSync, _ => new InvalidOperationException()),
            succeeded.Validate(validateAsync, _ => new InvalidOperationException()),
            succeeded.Validate(validateCancellable, _ => new InvalidOperationException())
        ];

        Type[] expectedTypes =
        [
            typeof(DoOnSuccess.Sync<int>),
            typeof(DoOnSuccess.Async<int>),
            typeof(DoOnSuccess.CancellableAsync<int>),
            typeof(DoOnFailure.Sync<int>),
            typeof(DoOnFailure.Async<int>),
            typeof(DoOnFailure.CancellableAsync<int>),
            typeof(Select.Sync<int, string>),
            typeof(Select.Async<int, string>),
            typeof(Select.CancellableAsync<int, string>),
            typeof(Chain.Sync<int, string>),
            typeof(Chain.Async<int, string>),
            typeof(Chain.CancellableAsync<int, string>),
            typeof(Validate.Sync<int>),
            typeof(Validate.Async<int>),
            typeof(Validate.CancellableAsync<int>)
        ];

        Assert.Equal(expectedTypes, flows.Select(static flow => flow.GetType()));
    }

    [Fact]
    public void RecoverOverloads_ProduceTheirDescriptionVariants()
    {
        var failed = Flow.Fail<int>(new InvalidOperationException("failure"));
        Flow.Operations.Recover.Sync<int> recoverSync = _ => Flow.Succeed(1);
        Flow.Operations.Recover.Async<int> recoverAsync =
            _ => Task.FromResult(Flow.Succeed(1));
        Func<Exception, CancellationToken, Task<IFlow<int>>> recoverCancellable =
            (_, _) => Task.FromResult(Flow.Succeed(1));

        Assert.IsType<Recover.Sync<int>>(failed.Recover(recoverSync));
        Assert.IsType<Recover.Async<int>>(failed.Recover(recoverAsync));
        Assert.IsType<Recover.CancellableAsync<int>>(failed.Recover(recoverCancellable));
    }

    [Fact]
    public async Task Chain_DefersRuntimeGeneratedDescriptionUntilPublicExecution()
    {
        var invocationCount = 0;
        var flow = Flow.Succeed(1).Chain(value =>
        {
            invocationCount++;
            return Flow.Succeed(value + 1);
        });

        Assert.IsType<Chain.Sync<int, int>>(flow);
        Assert.Equal(0, invocationCount);
        Assert.Equal(new Success<int>(2), await FlowEngine.ExecuteAsync(flow));
        Assert.Equal(1, invocationCount);
    }

    [Fact]
    public async Task Recover_DefersFallbackUntilPublicExecution()
    {
        var invocationCount = 0;
        var flow = Flow.Fail<int>(new InvalidOperationException("failure")).Recover(_ =>
        {
            invocationCount++;
            return Flow.Succeed(2);
        });

        Assert.IsType<Recover.Sync<int>>(flow);
        Assert.Equal(0, invocationCount);
        Assert.Equal(new Success<int>(2), await FlowEngine.ExecuteAsync(flow));
        Assert.Equal(1, invocationCount);
    }
}
