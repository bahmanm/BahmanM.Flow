
namespace BahmanM.Flow.Description.Tests.Unit;

public class CreateDescriptionTests
{
    [Fact]
    public async Task Create_Description_Defers_Operation_Until_Execution()
    {
        // Arrange
        var invocationCount = 0;
        Flow.Operations.Create.Sync<int> operation = () => ++invocationCount;
        var description = (Flow.Create(() => operation())).AsDescription();

        // Act
        var invocationCountBeforeExecution = invocationCount;
        var outcome = await FlowEngine.ExecuteAsync(description);

        // Assert
        Assert.Equal(0, invocationCountBeforeExecution);
        Assert.Equal(1, Assert.IsType<Success<int>>(outcome).Value);
        Assert.Equal(1, invocationCount);
        Assert.IsType<Ast.Create.Sync<int>>(description.Lower(BahmanM.Flow.FacadeBehaviourStrategyFactory.Instance));
    }

    [Fact]
    public async Task Create_Async_Description_Defers_Operation_Until_Execution()
    {
        // Arrange
        var invocationCount = 0;
        Flow.Operations.Create.Async<int> operation = () => Task.FromResult(++invocationCount);
        var description = (Flow.Create(() => operation())).AsDescription();

        // Act
        var invocationCountBeforeExecution = invocationCount;
        var outcome = await FlowEngine.ExecuteAsync(description);

        // Assert
        Assert.Equal(0, invocationCountBeforeExecution);
        Assert.Equal(1, Assert.IsType<Success<int>>(outcome).Value);
        Assert.Equal(1, invocationCount);
        Assert.IsType<Ast.Create.Async<int>>(description.Lower(BahmanM.Flow.FacadeBehaviourStrategyFactory.Instance));
    }

    [Fact]
    public async Task Create_Async_Description_Captures_Operation_Exception()
    {
        // Arrange
        var exception = new InvalidOperationException("async failure");
        Flow.Operations.Create.Async<int> operation = () => Task.FromException<int>(exception);
        var description = (Flow.Create(() => operation())).AsDescription();

        // Act
        var outcome = await FlowEngine.ExecuteAsync(description);

        // Assert
        Assert.Same(exception, Assert.IsType<Failure<int>>(outcome).Exception);
    }

    [Fact]
    public async Task Create_CancellableAsync_Description_Passes_Execution_Cancellation_Token()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var observedToken = CancellationToken.None;
        Flow.Operations.Create.CancellableAsync<int> operation = cancellationToken =>
        {
            observedToken = cancellationToken;
            return Task.FromResult(1);
        };
        var description = (Flow.Create(cancellationToken => operation(cancellationToken))).AsDescription();

        // Act
        var outcome = await FlowEngine.ExecuteAsync(description, new Execution.Options(cancellationTokenSource.Token));

        // Assert
        Assert.Equal(cancellationTokenSource.Token, observedToken);
        Assert.Equal(1, Assert.IsType<Success<int>>(outcome).Value);
        Assert.IsType<Ast.Create.CancellableAsync<int>>(description.Lower(BahmanM.Flow.FacadeBehaviourStrategyFactory.Instance));
    }

    [Fact]
    public async Task Create_CancellableAsync_Description_Does_Not_Invoke_Operation_When_Already_Cancelled()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        var invocationCount = 0;
        Flow.Operations.Create.CancellableAsync<int> operation = _ =>
        {
            invocationCount++;
            return Task.FromResult(1);
        };
        var description = (Flow.Create(cancellationToken => operation(cancellationToken))).AsDescription();

        // Act
        var outcome = await FlowEngine.ExecuteAsync(description, new Execution.Options(cancellationTokenSource.Token));

        // Assert
        Assert.IsAssignableFrom<OperationCanceledException>(Assert.IsType<Failure<int>>(outcome).Exception);
        Assert.Equal(0, invocationCount);
    }
}
