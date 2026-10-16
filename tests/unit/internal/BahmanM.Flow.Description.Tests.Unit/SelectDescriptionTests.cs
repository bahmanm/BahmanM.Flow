
namespace BahmanM.Flow.Description.Tests.Unit;

public class SelectDescriptionTests
{
    [Fact]
    public async Task Select_Description_Lowers_And_Defers_Operation()
    {
        // Arrange
        var invocationCount = 0;
        Flow.Operations.Select.Sync<string, int> operation = value =>
        {
            invocationCount++;
            return value.Length;
        };
        var description = (((Flow.Succeed("Hello")).AsDescription()).Select(operation)).AsDescription();
        var expectedFlow = Flow.Succeed("Hello").Select(value => value.Length);

        // Act
        var invocationCountBeforeExecution = invocationCount;
        var actual = await FlowEngine.ExecuteAsync(description);
        var expected = await FlowEngine.ExecuteAsync(expectedFlow);

        // Assert
        Assert.Equal(0, invocationCountBeforeExecution);
        Assert.Equal(expected, actual);
        Assert.Equal(1, invocationCount);
        Assert.IsType<Ast.Select.Sync<string, int>>(description.Lower(FacadeBehaviourStrategyFactory.Instance));
    }

    [Fact]
    public async Task Select_Async_Description_Lowers_And_Defers_Operation()
    {
        // Arrange
        var invocationCount = 0;
        Flow.Operations.Select.Async<string, int> operation = value =>
        {
            invocationCount++;
            return Task.FromResult(value.Length);
        };
        var description = (((Flow.Succeed("Hello")).AsDescription()).Select(operation)).AsDescription();
        Flow.Operations.Select.Async<string, int> expectedOperation = value => Task.FromResult(value.Length);
        var expectedFlow = Flow.Succeed("Hello").Select(expectedOperation);

        // Act
        var invocationCountBeforeExecution = invocationCount;
        var actual = await FlowEngine.ExecuteAsync(description);
        var expected = await FlowEngine.ExecuteAsync(expectedFlow);

        // Assert
        Assert.Equal(0, invocationCountBeforeExecution);
        Assert.Equal(expected, actual);
        Assert.Equal(1, invocationCount);
        Assert.IsType<Ast.Select.Async<string, int>>(description.Lower(FacadeBehaviourStrategyFactory.Instance));
    }

    [Fact]
    public async Task Select_CancellableAsync_Description_Passes_Execution_Cancellation_Token()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var observedToken = CancellationToken.None;
        Flow.Operations.Select.CancellableAsync<string, int> operation = (value, cancellationToken) =>
        {
            observedToken = cancellationToken;
            return Task.FromResult(value.Length);
        };
        var description = (((Flow.Succeed("Hello")).AsDescription()).Select(operation)).AsDescription();

        // Act
        var outcome = await FlowEngine.ExecuteAsync(description, new Execution.Options(cancellationTokenSource.Token));

        // Assert
        Assert.Equal(cancellationTokenSource.Token, observedToken);
        Assert.Equal(5, Assert.IsType<Success<int>>(outcome).Value);
        Assert.IsType<Ast.Select.CancellableAsync<string, int>>(description.Lower(FacadeBehaviourStrategyFactory.Instance));
    }
}
