using BahmanM.Flow.Description.Rendering;

namespace BahmanM.Flow.Description.Tests.Unit;

public class DoOnSuccessDescriptionTests
{
    [Fact]
    public async Task DoOnSuccess_Sync_Description_Runs_Action_And_Preserves_Success()
    {
        // Arrange
        var actionCalls = 0;
        Flow.Operations.DoOnSuccess.Sync<int> action = value =>
        {
            Assert.Equal(42, value);
            actionCalls++;
        };
        var description = (((Flow.Succeed(42)).AsDescription()).DoOnSuccess(action)).AsDescription();

        // Act
        var descriptionText = Renderer.Describe(description);
        var callsBeforeExecution = actionCalls;
        var lowered = description.Lower(FacadeBehaviourStrategyFactory.Instance);
        var outcome = await FlowEngine.ExecuteAsync(description);
        var expectedOutcome = await FlowEngine.ExecuteAsync(
            Flow.Succeed(42).DoOnSuccess(_ => { }));

        // Assert
        Assert.Equal(expectedOutcome, outcome);
        Assert.Equal(Outcome.Success(42), outcome);
        Assert.Equal(0, callsBeforeExecution);
        Assert.IsType<Ast.DoOnSuccess.Sync<int>>(lowered);
        Assert.Equal(1, actionCalls);
        Assert.Contains("action: lambda(int) -> void", descriptionText);
    }

    [Fact]
    public async Task DoOnSuccess_Sync_Description_Turns_Action_Exception_Into_Failure()
    {
        // Arrange
        var expected = new InvalidOperationException("action");
        Flow.Operations.DoOnSuccess.Sync<int> action = _ => throw expected;
        var description = (((Flow.Succeed(42)).AsDescription()).DoOnSuccess(action)).AsDescription();

        // Act
        var outcome = await FlowEngine.ExecuteAsync(description);
        var expectedOutcome = await FlowEngine.ExecuteAsync(
            Flow.Succeed(42).DoOnSuccess((Flow.Operations.DoOnSuccess.Sync<int>)(_ => throw expected)));

        // Assert
        Assert.Equal(expectedOutcome, outcome);
        Assert.Same(expected, Assert.IsType<Failure<int>>(outcome).Exception);
    }

    [Fact]
    public async Task DoOnSuccess_Sync_Description_Skips_Action_And_Preserves_Upstream_Failure()
    {
        // Arrange
        var expected = new InvalidOperationException("upstream");
        var actionCalls = 0;
        Flow.Operations.DoOnSuccess.Sync<int> action = _ => actionCalls++;
        var description = (((Flow.Fail<int>(expected)).AsDescription()).DoOnSuccess(action)).AsDescription();

        // Act
        var outcome = await FlowEngine.ExecuteAsync(description);
        var expectedOutcome = await FlowEngine.ExecuteAsync(
            Flow.Fail<int>(expected).DoOnSuccess(_ => { }));

        // Assert
        Assert.Equal(expectedOutcome, outcome);
        Assert.Same(expected, Assert.IsType<Failure<int>>(outcome).Exception);
        Assert.Equal(0, actionCalls);
    }

    [Fact]
    public async Task DoOnSuccess_Async_Description_Runs_Action_And_Preserves_Success()
    {
        // Arrange
        var actionCalls = 0;
        Flow.Operations.DoOnSuccess.Async<int> action = async value =>
        {
            Assert.Equal(42, value);
            await Task.Yield();
            actionCalls++;
        };
        var description = (((Flow.Succeed(42)).AsDescription()).DoOnSuccess(action)).AsDescription();

        // Act
        var descriptionText = Renderer.Describe(description);
        var actionCallsBeforeExecution = actionCalls;
        var outcome = await FlowEngine.ExecuteAsync(description);
        var expected = await FlowEngine.ExecuteAsync(
            Flow.Succeed(42).DoOnSuccess(_ => Task.CompletedTask));

        // Assert
        Assert.Equal(expected, outcome);
        Assert.Equal(Outcome.Success(42), outcome);
        Assert.Equal(0, actionCallsBeforeExecution);
        Assert.Equal(1, actionCalls);
        Assert.Contains("DoOnSuccess.Async<int>", descriptionText);
        Assert.Contains("action: lambda(int) -> Task", descriptionText);
        Assert.IsType<Ast.DoOnSuccess.Async<int>>(description.Lower(FacadeBehaviourStrategyFactory.Instance));
    }

    [Fact]
    public async Task DoOnSuccess_CancellableAsync_Description_Passes_Execution_Token()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var observedToken = CancellationToken.None;
        Flow.Operations.DoOnSuccess.CancellableAsync<int> action = (_, token) =>
        {
            observedToken = token;
            return Task.CompletedTask;
        };
        var description = (((Flow.Succeed(42)).AsDescription()).DoOnSuccess(action)).AsDescription();
        var options = new Execution.Options(cancellationTokenSource.Token);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(description, options);
        var expected = await FlowEngine.ExecuteAsync(
            Flow.Succeed(42).DoOnSuccess(
                (_, _) => Task.CompletedTask), options);

        // Assert
        Assert.Equal(expected, outcome);
        Assert.Equal(Outcome.Success(42), outcome);
        Assert.Equal(cancellationTokenSource.Token, observedToken);
        Assert.IsType<Ast.DoOnSuccess.CancellableAsync<int>>(description.Lower(FacadeBehaviourStrategyFactory.Instance));
    }
}
