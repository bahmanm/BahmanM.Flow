using BahmanM.Flow.Description.Formatting;

namespace BahmanM.Flow.Description.Tests.Unit;

public class AsyncChainAndRecoverDescriptionTests
{
    [Fact]
    public async Task Chain_Async_Description_Lowers_And_Defers_Callback_And_Child()
    {
        // Arrange
        var callbackCalls = 0;
        var childCalls = 0;
        Flow.Operations.Chain.Async<int, int> operation = async value =>
        {
            callbackCalls++;
            await Task.Yield();
            return (Flow.Create(() =>
            {
                childCalls++;
                return value + 1;
            })).AsDescription();
        };
        var description = (((Flow.Succeed(41)).AsDescription()).Chain(operation)).AsDescription();

        // Act
        var descriptionText = DescriptionFormatting.Describe(description);
        var lowered = description.Lower(FacadeBehaviourStrategyFactory.Instance);
        var callbackCallsBeforeExecution = callbackCalls;
        var childCallsBeforeExecution = childCalls;
        var outcome = await FlowEngine.ExecuteAsync(description);
        var expected = await FlowEngine.ExecuteAsync(
            Flow.Succeed(41).Chain(async value =>
            {
                await Task.Yield();
                return Flow.Create(() => value + 1);
            }));

        // Assert
        Assert.Contains("Chain.Async<int, int>", descriptionText);
        Assert.Contains("operation: lambda(int) -> Task<IFlow<int>>", descriptionText);
        Assert.Equal(0, callbackCallsBeforeExecution);
        Assert.Equal(0, childCallsBeforeExecution);
        Assert.Equal(1, callbackCalls);
        Assert.Equal(1, childCalls);
        Assert.Equal(expected, outcome);
        Assert.Equal(Outcome.Success(42), outcome);
        Assert.IsType<Ast.Chain.Async<int, int>>(lowered);
    }

    [Fact]
    public async Task Chain_Async_Description_Skips_Callback_On_Upstream_Failure()
    {
        // Arrange
        var expectedException = new InvalidOperationException("upstream");
        var callbackCalls = 0;
        Flow.Operations.Chain.Async<int, int> operation = _ =>
        {
            callbackCalls++;
            return Task.FromResult<IFlow<int>>((Flow.Succeed(0)).AsDescription());
        };
        var description = (((Flow.Fail<int>(expectedException)).AsDescription()).Chain(operation)).AsDescription();

        // Act
        var outcome = await FlowEngine.ExecuteAsync(description);
        var expected = await FlowEngine.ExecuteAsync(
            Flow.Fail<int>(expectedException).Chain(_ => Task.FromResult(Flow.Succeed(0))));

        // Assert
        Assert.Equal(expected, outcome);
        Assert.Same(expectedException, Assert.IsType<Failure<int>>(outcome).Exception);
        Assert.Equal(0, callbackCalls);
    }

    [Fact]
    public async Task Chain_Async_Description_Captures_Callback_Exception()
    {
        // Arrange
        var expectedException = new InvalidOperationException("callback");
        Flow.Operations.Chain.Async<int, int> operation = _ => Task.FromException<IFlow<int>>(expectedException);
        var description = (((Flow.Succeed(1)).AsDescription()).Chain(operation)).AsDescription();

        // Act
        var outcome = await FlowEngine.ExecuteAsync(description);
        var expected = await FlowEngine.ExecuteAsync(
            Flow.Succeed(1).Chain(_ => Task.FromException<IFlow<int>>(expectedException)));

        // Assert
        Assert.Equal(expected, outcome);
        Assert.Same(expectedException, Assert.IsType<Failure<int>>(outcome).Exception);
    }

    [Fact]
    public async Task Chain_CancellableAsync_Description_Passes_Token_And_Lowers_Nested_Child()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var callbackCalls = 0;
        var childCalls = 0;
        var observedToken = CancellationToken.None;
        Flow.Operations.Chain.CancellableAsync<int, int> operation = async (value, token) =>
        {
            callbackCalls++;
            observedToken = token;
            await Task.Yield();
            return (((Flow.Create(() =>
                {
                    childCalls++;
                    return value + 1;
                })).AsDescription()).Select(result => result * 2)).AsDescription();
        };
        var description = (((Flow.Succeed(20)).AsDescription()).Chain(operation)).AsDescription();
        var options = new Execution.Options(cancellationTokenSource.Token);

        // Act
        var descriptionText = DescriptionFormatting.Describe(description);
        var outcome = await FlowEngine.ExecuteAsync(description, options);
        Flow.Operations.Chain.CancellableAsync<int, int> expectedOperation = async (value, _) =>
        {
            await Task.Yield();
            return Flow.Create(() => value + 1).Select(result => result * 2);
        };
        var expected = await FlowEngine.ExecuteAsync(
            Flow.Succeed(20).Chain(expectedOperation), options);

        // Assert
        Assert.Contains("Chain.CancellableAsync<int, int>", descriptionText);
        Assert.Contains("operation: lambda(int, CancellationToken) -> Task<IFlow<int>>", descriptionText);
        Assert.Equal(cancellationTokenSource.Token, observedToken);
        Assert.Equal(1, callbackCalls);
        Assert.Equal(1, childCalls);
        Assert.Equal(expected, outcome);
        Assert.Equal(Outcome.Success(42), outcome);
        Assert.IsType<Ast.Chain.CancellableAsync<int, int>>(description.Lower(FacadeBehaviourStrategyFactory.Instance));
    }

    [Fact]
    public async Task Chain_CancellableAsync_Description_Skips_Callback_When_Already_Cancelled()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        var callbackCalls = 0;
        Flow.Operations.Chain.CancellableAsync<int, int> operation = (_, _) =>
        {
            callbackCalls++;
            return Task.FromResult<IFlow<int>>((Flow.Succeed(42)).AsDescription());
        };
        var description = (((Flow.Succeed(1)).AsDescription()).Chain(operation)).AsDescription();

        // Act
        var outcome = await FlowEngine.ExecuteAsync(
            description, new Execution.Options(cancellationTokenSource.Token));

        // Assert
        Assert.IsType<Failure<int>>(outcome);
        Assert.IsType<TaskCanceledException>(Assert.IsType<Failure<int>>(outcome).Exception);
        Assert.Equal(0, callbackCalls);
    }

    [Fact]
    public async Task Chain_CancellableAsync_Description_Captures_Callback_Exception()
    {
        // Arrange
        var expectedException = new InvalidOperationException("callback");
        Flow.Operations.Chain.CancellableAsync<int, int> operation = (_, _) =>
            Task.FromException<IFlow<int>>(expectedException);
        Flow.Operations.Chain.CancellableAsync<int, int> expectedOperation = (_, _) =>
            Task.FromException<IFlow<int>>(expectedException);
        var description = (((Flow.Succeed(1)).AsDescription()).Chain(operation)).AsDescription();

        // Act
        var outcome = await FlowEngine.ExecuteAsync(description);
        var expected = await FlowEngine.ExecuteAsync(Flow.Succeed(1).Chain(expectedOperation));

        // Assert
        Assert.Equal(expected, outcome);
        Assert.Equal(expectedException, Assert.IsType<Failure<int>>(outcome).Exception);
    }

    [Fact]
    public async Task Recover_Async_Description_Lowers_And_Defers_Callback_And_Child()
    {
        // Arrange
        var expectedException = new InvalidOperationException("upstream");
        var callbackCalls = 0;
        var childCalls = 0;
        Flow.Operations.Recover.Async<int> recover = async exception =>
        {
            callbackCalls++;
            await Task.Yield();
            return (Flow.Create(() =>
            {
                childCalls++;
                return exception.Message.Length;
            })).AsDescription();
        };
        var description = (((Flow.Fail<int>(expectedException)).AsDescription()).Recover(recover)).AsDescription();

        // Act
        var descriptionText = DescriptionFormatting.Describe(description);
        var lowered = description.Lower(FacadeBehaviourStrategyFactory.Instance);
        var callbackCallsBeforeExecution = callbackCalls;
        var childCallsBeforeExecution = childCalls;
        var outcome = await FlowEngine.ExecuteAsync(description);
        var expected = await FlowEngine.ExecuteAsync(
            Flow.Fail<int>(expectedException).Recover(async error =>
            {
                await Task.Yield();
                return Flow.Create(() => error.Message.Length);
            }));

        // Assert
        Assert.Contains("Recover.Async<int>", descriptionText);
        Assert.Contains("recover: lambda(Exception) -> Task<IFlow<int>>", descriptionText);
        Assert.Equal(0, callbackCallsBeforeExecution);
        Assert.Equal(0, childCallsBeforeExecution);
        Assert.Equal(1, callbackCalls);
        Assert.Equal(1, childCalls);
        Assert.Equal(expected, outcome);
        Assert.Equal(Outcome.Success(expectedException.Message.Length), outcome);
        Assert.IsType<Ast.Recover.Async<int>>(lowered);
    }

    [Fact]
    public async Task Recover_Async_Description_Skips_Callback_On_Upstream_Success()
    {
        // Arrange
        var callbackCalls = 0;
        Flow.Operations.Recover.Async<int> recover = _ =>
        {
            callbackCalls++;
            return Task.FromResult<IFlow<int>>((Flow.Succeed(0)).AsDescription());
        };
        var description = (((Flow.Succeed(42)).AsDescription()).Recover(recover)).AsDescription();

        // Act
        var outcome = await FlowEngine.ExecuteAsync(description);
        var expected = await FlowEngine.ExecuteAsync(
            Flow.Succeed(42).Recover(_ => Task.FromResult(Flow.Succeed(0))));

        // Assert
        Assert.Equal(expected, outcome);
        Assert.Equal(Outcome.Success(42), outcome);
        Assert.Equal(0, callbackCalls);
    }

    [Fact]
    public async Task Recover_Async_Description_Captures_Callback_Exception()
    {
        // Arrange
        var expectedException = new InvalidOperationException("callback");
        Flow.Operations.Recover.Async<int> recover = _ => Task.FromException<IFlow<int>>(expectedException);
        var description = (((Flow.Fail<int>(new Exception("upstream"))).AsDescription()).Recover(recover)).AsDescription();

        // Act
        var outcome = await FlowEngine.ExecuteAsync(description);
        var expected = await FlowEngine.ExecuteAsync(
            Flow.Fail<int>(new Exception("upstream")).Recover(_ => Task.FromException<IFlow<int>>(expectedException)));

        // Assert
        Assert.Equal(expected, outcome);
        Assert.Same(expectedException, Assert.IsType<Failure<int>>(outcome).Exception);
    }

    [Fact]
    public async Task Recover_CancellableAsync_Description_Passes_Token_And_Skips_Callback_On_Success()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var callbackCalls = 0;
        var observedToken = CancellationToken.None;
        Func<Exception, CancellationToken, Task<IFlow<int>>> recover = async (_, token) =>
        {
            callbackCalls++;
            observedToken = token;
            await Task.Yield();
            return (Flow.Succeed(0)).AsDescription();
        };
        var failed = (((Flow.Fail<int>(new Exception("upstream"))).AsDescription()).Recover(recover)).AsDescription();
        var succeeded = (((Flow.Succeed(42)).AsDescription()).Recover(recover)).AsDescription();
        var options = new Execution.Options(cancellationTokenSource.Token);

        // Act
        var descriptionText = DescriptionFormatting.Describe(failed);
        var outcome = await FlowEngine.ExecuteAsync(failed, options);
        var successOutcome = await FlowEngine.ExecuteAsync(succeeded, options);
        Func<Exception, CancellationToken, Task<IFlow<int>>> expectedRecover = async (_, _) =>
        {
            await Task.Yield();
            return Flow.Succeed(0);
        };
        var expected = await FlowEngine.ExecuteAsync(
            Flow.Fail<int>(new Exception("upstream")).Recover(expectedRecover), options);

        // Assert
        Assert.Contains("Recover.CancellableAsync<int>", descriptionText);
        Assert.Contains("recover: lambda(Exception, CancellationToken) -> Task<IFlow<int>>", descriptionText);
        Assert.Equal(cancellationTokenSource.Token, observedToken);
        Assert.Equal(1, callbackCalls);
        Assert.Equal(expected, outcome);
        Assert.Equal(Outcome.Success(0), outcome);
        Assert.Equal(Outcome.Success(42), successOutcome);
        Assert.IsType<Ast.Recover.CancellableAsync<int>>(failed.Lower(FacadeBehaviourStrategyFactory.Instance));
    }

    [Fact]
    public async Task Recover_CancellableAsync_Description_Captures_Callback_Exception()
    {
        // Arrange
        var expectedException = new InvalidOperationException("callback");
        Func<Exception, CancellationToken, Task<IFlow<int>>> recover = (_, _) =>
            Task.FromException<IFlow<int>>(expectedException);
        var description = (((Flow.Fail<int>(new Exception("upstream"))).AsDescription()).Recover(recover)).AsDescription();

        // Act
        var outcome = await FlowEngine.ExecuteAsync(description);

        // Assert
        Assert.Same(expectedException, Assert.IsType<Failure<int>>(outcome).Exception);
    }
}
