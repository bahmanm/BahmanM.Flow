using BahmanM.Flow.Description.Serialisation;
using BahmanM.Flow.Description.Tests.Unit.Support;

namespace BahmanM.Flow.Description.Tests.Unit;

public class DoOnFailureDescriptionTests
{
    [Fact]
    public async Task DoOnFailure_Sync_Description_Runs_Action_And_Preserves_Original_Failure()
    {
        // Arrange
        var expected = new InvalidOperationException("upstream");
        Exception? observed = null;
        Flow.Operations.DoOnFailure.Sync action = exception => observed = exception;
        var description = DescriptionFlowBuilder.DoOnFailure(DescriptionFlowBuilder.Fail<int>(expected), action);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(description);
        var expectedOutcome = await FlowEngine.ExecuteAsync(
            Flow.Fail<int>(expected).DoOnFailure(_ => { }));

        // Assert
        Assert.Same(expected, observed);
        Assert.Equal(expectedOutcome, outcome);
        Assert.Same(expected, Assert.IsType<Failure<int>>(outcome).Exception);
        Assert.IsType<Ast.DoOnFailure.Sync<int>>(description.Lower());
    }

    [Fact]
    public async Task DoOnFailure_Sync_Description_Swallows_Action_Exception()
    {
        // Arrange
        var expected = new InvalidOperationException("upstream");
        Flow.Operations.DoOnFailure.Sync action = _ => throw new Exception("observer");
        var description = DescriptionFlowBuilder.DoOnFailure(DescriptionFlowBuilder.Fail<int>(expected), action);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(description);
        var expectedOutcome = await FlowEngine.ExecuteAsync(
            Flow.Fail<int>(expected).DoOnFailure((Flow.Operations.DoOnFailure.Sync)(_ => throw new Exception("observer"))));

        // Assert
        Assert.Equal(expectedOutcome, outcome);
        Assert.Same(expected, Assert.IsType<Failure<int>>(outcome).Exception);
    }

    [Fact]
    public async Task DoOnFailure_Sync_Description_Skips_Action_And_Preserves_Upstream_Success()
    {
        // Arrange
        var actionCalls = 0;
        Flow.Operations.DoOnFailure.Sync action = _ => actionCalls++;
        var description = DescriptionFlowBuilder.DoOnFailure(DescriptionFlowBuilder.Succeed(42), action);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(description);
        var expectedOutcome = await FlowEngine.ExecuteAsync(
            Flow.Succeed(42).DoOnFailure(_ => { }));

        // Assert
        Assert.Equal(expectedOutcome, outcome);
        Assert.Equal(Outcome.Success(42), outcome);
        Assert.Equal(0, actionCalls);
    }

    [Fact]
    public async Task DoOnFailure_Async_Description_Preserves_Original_Failure_When_Action_Throws()
    {
        // Arrange
        var expectedException = new InvalidOperationException("upstream");
        Exception? observedException = null;
        Flow.Operations.DoOnFailure.Async action = async exception =>
        {
            observedException = exception;
            await Task.Yield();
            throw new InvalidOperationException("observer");
        };
        var description = DescriptionFlowBuilder.DoOnFailure(
            DescriptionFlowBuilder.Fail<int>(expectedException), action);

        // Act
        var descriptionText = DescriptionFormatter.Describe(description);
        var outcome = await FlowEngine.ExecuteAsync(description);
        var expected = await FlowEngine.ExecuteAsync(
            Flow.Fail<int>(expectedException).DoOnFailure(
                _ => Task.FromException(new InvalidOperationException("observer"))));

        // Assert
        Assert.Equal(expected, outcome);
        Assert.Same(expectedException, observedException);
        Assert.Same(expectedException, Assert.IsType<Failure<int>>(outcome).Exception);
        Assert.Contains("DoOnFailure.Async<int>", descriptionText);
        Assert.Contains("action: lambda(Exception) -> Task", descriptionText);
        Assert.IsType<Ast.DoOnFailure.Async<int>>(description.Lower());
    }

    [Fact]
    public async Task DoOnFailure_CancellableAsync_Description_Preserves_Original_Failure_When_Action_Is_Cancelled()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        var expectedException = new InvalidOperationException("upstream");
        var observedToken = CancellationToken.None;
        Flow.Operations.DoOnFailure.CancellableAsync action = async (_, token) =>
        {
            observedToken = token;
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
        };
        var description = DescriptionFlowBuilder.DoOnFailure(
            DescriptionFlowBuilder.Fail<int>(expectedException), action);
        var options = new Execution.Options(cancellationTokenSource.Token);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(description, options);
        var expected = await FlowEngine.ExecuteAsync(
            Flow.Fail<int>(expectedException).DoOnFailure(
                (_, token) => Task.Delay(Timeout.InfiniteTimeSpan, token)),
            options);

        // Assert
        Assert.Equal(expected, outcome);
        Assert.Same(expectedException, Assert.IsType<Failure<int>>(outcome).Exception);
        Assert.Equal(cancellationTokenSource.Token, observedToken);
        Assert.IsType<Ast.DoOnFailure.CancellableAsync<int>>(description.Lower());
    }
}
