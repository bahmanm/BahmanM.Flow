using BahmanM.Flow.Description.Serialisation;
using BahmanM.Flow.Description.Tests.Unit.Support;

namespace BahmanM.Flow.Description.Tests.Unit;

public class ChainAndRecoverDescriptionTests
{
    [Fact]
    public async Task Chain_Sync_Description_Lowers_And_Defers_Callback_And_Child()
    {
        // Arrange
        var callbackCalls = 0;
        var childCalls = 0;
        Flow.Operations.Chain.Sync<int, int> operation = value =>
        {
            callbackCalls++;
            return DescriptionFlowBuilder.Create(() =>
            {
                childCalls++;
                return value + 1;
            });
        };
        var description = DescriptionFlowBuilder.Chain(DescriptionFlowBuilder.Succeed(41), operation);

        // Act
        var descriptionText = DescriptionFormatter.Describe(description);
        var lowered = description.Lower();
        var callbackCallsBeforeExecution = callbackCalls;
        var childCallsBeforeExecution = childCalls;
        var outcome = await FlowEngine.ExecuteAsync(description);
        var expected = await FlowEngine.ExecuteAsync(
            Flow.Succeed(41).Chain(value => Flow.Create(() => value + 1)));

        // Assert
        Assert.Contains("Chain.Sync<int, int>", descriptionText);
        Assert.Contains("operation: lambda(int) -> IFlow<int>", descriptionText);
        Assert.Equal(0, callbackCallsBeforeExecution);
        Assert.Equal(0, childCallsBeforeExecution);
        Assert.Equal(1, callbackCalls);
        Assert.Equal(1, childCalls);
        Assert.Equal(expected, outcome);
        Assert.Equal(Outcome.Success(42), outcome);
        Assert.IsType<Ast.Chain.Sync<int, int>>(lowered);
    }

    [Fact]
    public async Task Chain_Sync_Description_Skips_Callback_And_Preserves_Upstream_Failure()
    {
        // Arrange
        var expectedException = new InvalidOperationException("upstream");
        var callbackCalls = 0;
        Flow.Operations.Chain.Sync<int, int> operation = _ =>
        {
            callbackCalls++;
            return DescriptionFlowBuilder.Succeed(42);
        };
        var description = DescriptionFlowBuilder.Chain(
            DescriptionFlowBuilder.Fail<int>(expectedException), operation);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(description);
        var expected = await FlowEngine.ExecuteAsync(
            Flow.Fail<int>(expectedException).Chain(Flow.Succeed));

        // Assert
        Assert.Equal(expected, outcome);
        Assert.Same(expectedException, Assert.IsType<Failure<int>>(outcome).Exception);
        Assert.Equal(0, callbackCalls);
    }

    [Fact]
    public async Task Chain_Sync_Description_Captures_Callback_Exception()
    {
        // Arrange
        var expectedException = new InvalidOperationException("callback");
        Flow.Operations.Chain.Sync<int, int> operation = _ => throw expectedException;
        var description = DescriptionFlowBuilder.Chain(DescriptionFlowBuilder.Succeed(1), operation);
        Flow.Operations.Chain.Sync<int, int> expectedOperation = _ => throw expectedException;

        // Act
        var outcome = await FlowEngine.ExecuteAsync(description);
        var expected = await FlowEngine.ExecuteAsync(Flow.Succeed(1).Chain(expectedOperation));

        // Assert
        Assert.Equal(expected, outcome);
        Assert.Same(expectedException, Assert.IsType<Failure<int>>(outcome).Exception);
    }

    [Fact]
    public async Task Recover_Sync_Description_Lowers_And_Defers_Callback_And_Nested_Child()
    {
        // Arrange
        var expectedException = new InvalidOperationException("upstream");
        var callbackCalls = 0;
        var childCalls = 0;
        Flow.Operations.Recover.Sync<int> recover = exception =>
        {
            callbackCalls++;
            return DescriptionFlowBuilder.Select(
                DescriptionFlowBuilder.Create(() =>
                {
                    childCalls++;
                    return exception.Message.Length;
                }),
                length => length + 1);
        };
        var description = DescriptionFlowBuilder.Recover(
            DescriptionFlowBuilder.Fail<int>(expectedException), recover);

        // Act
        var descriptionText = DescriptionFormatter.Describe(description);
        var lowered = description.Lower();
        var callbackCallsBeforeExecution = callbackCalls;
        var childCallsBeforeExecution = childCalls;
        var outcome = await FlowEngine.ExecuteAsync(description);
        var expected = await FlowEngine.ExecuteAsync(
            Flow.Fail<int>(expectedException).Recover(error =>
                Flow.Create(() => error.Message.Length).Select(length => length + 1)));

        // Assert
        Assert.Contains("Recover.Sync<int>", descriptionText);
        Assert.Contains("recover: lambda(Exception) -> IFlow<int>", descriptionText);
        Assert.Equal(0, callbackCallsBeforeExecution);
        Assert.Equal(0, childCallsBeforeExecution);
        Assert.Equal(1, callbackCalls);
        Assert.Equal(1, childCalls);
        Assert.Equal(expected, outcome);
        Assert.Equal(Outcome.Success(expectedException.Message.Length + 1), outcome);
        Assert.IsType<Ast.Recover.Sync<int>>(lowered);
    }

    [Fact]
    public async Task Recover_Sync_Description_Skips_Callback_And_Preserves_Upstream_Success()
    {
        // Arrange
        var callbackCalls = 0;
        Flow.Operations.Recover.Sync<int> recover = _ =>
        {
            callbackCalls++;
            return DescriptionFlowBuilder.Succeed(0);
        };
        var description = DescriptionFlowBuilder.Recover(DescriptionFlowBuilder.Succeed(42), recover);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(description);
        var expected = await FlowEngine.ExecuteAsync(
            Flow.Succeed(42).Recover(_ => Flow.Succeed(0)));

        // Assert
        Assert.Equal(expected, outcome);
        Assert.Equal(Outcome.Success(42), outcome);
        Assert.Equal(0, callbackCalls);
    }

    [Fact]
    public async Task Recover_Sync_Description_Captures_Callback_Exception()
    {
        // Arrange
        var expectedException = new InvalidOperationException("recovery callback");
        Flow.Operations.Recover.Sync<int> recover = _ => throw expectedException;
        Flow.Operations.Recover.Sync<int> expectedRecover = _ => throw expectedException;
        var description = DescriptionFlowBuilder.Recover(
            DescriptionFlowBuilder.Fail<int>(new Exception("upstream")), recover);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(description);
        var expected = await FlowEngine.ExecuteAsync(
            Flow.Fail<int>(new Exception("upstream")).Recover(expectedRecover));

        // Assert
        Assert.Equal(expected, outcome);
        Assert.Same(expectedException, Assert.IsType<Failure<int>>(outcome).Exception);
    }
}
