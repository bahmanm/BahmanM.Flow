using BahmanM.Flow.Description.Serialisation;

namespace BahmanM.Flow.Description.Tests.Unit;

public class ValidateDescriptionTests
{
    [Fact]
    public async Task Validate_Sync_Description_Lowers_And_Defers_Both_Callbacks()
    {
        // Arrange
        var predicateCalls = 0;
        var factoryCalls = 0;
        Func<int, bool> predicate = value =>
        {
            predicateCalls++;
            return value > 0;
        };
        Func<int, Exception> exceptionFactory = _ =>
        {
            factoryCalls++;
            return new InvalidOperationException("invalid");
        };
        var description = (((Flow.Succeed(42)).AsDescription()).Validate(predicate, exceptionFactory)).AsDescription();

        // Act
        _ = DescriptionFormatter.Describe(description);
        var lowered = description.Lower(FacadeBehaviourStrategyFactory.Instance);
        var predicateCallsBeforeExecution = predicateCalls;
        var factoryCallsBeforeExecution = factoryCalls;

        var outcome = await FlowEngine.ExecuteAsync(description);
        var expected = await FlowEngine.ExecuteAsync(
            Flow.Succeed(42).Validate(_ => true, _ => new InvalidOperationException("unused")));

        // Assert
        Assert.Equal(expected, outcome);
        Assert.Equal(Outcome.Success(42), outcome);
        Assert.IsType<Ast.Validate.Sync<int>>(lowered);
        Assert.Equal(0, predicateCallsBeforeExecution);
        Assert.Equal(0, factoryCallsBeforeExecution);
        Assert.Equal(1, predicateCalls);
        Assert.Equal(0, factoryCalls);
    }

    [Fact]
    public async Task Validate_Sync_Description_Preserves_Factory_Exception_And_Describes_Both_Callbacks()
    {
        // Arrange
        var expected = new InvalidOperationException("invalid");
        var description = (((Flow.Succeed(0)).AsDescription()).Validate(_ => false, _ => expected)).AsDescription();

        // Act
        var descriptionText = DescriptionFormatter.Describe(description);
        var outcome = await FlowEngine.ExecuteAsync(description);
        var expectedOutcome = await FlowEngine.ExecuteAsync(
            Flow.Succeed(0).Validate(_ => false, _ => expected));

        // Assert
        Assert.Contains("predicate: lambda(int) -> bool", descriptionText);
        Assert.Contains("exceptionFactory: lambda(int) -> Exception", descriptionText);
        Assert.Equal(expectedOutcome, outcome);
        Assert.Same(expected, Assert.IsType<Failure<int>>(outcome).Exception);
        Assert.IsType<Ast.Validate.Sync<int>>(description.Lower(FacadeBehaviourStrategyFactory.Instance));
    }

    [Fact]
    public async Task Validate_Sync_Description_Passes_Through_Upstream_Failure_Without_Calling_Callbacks()
    {
        // Arrange
        var expected = new InvalidOperationException("upstream");
        var callbackCalls = 0;
        var description = (((Flow.Fail<int>(expected)).AsDescription()).Validate(_ => { callbackCalls++; return true; }, _ => { callbackCalls++; return new Exception("unused"); })).AsDescription();

        // Act
        var outcome = await FlowEngine.ExecuteAsync(description);
        var expectedOutcome = await FlowEngine.ExecuteAsync(
            Flow.Fail<int>(expected).Validate(_ => true, _ => new Exception("unused")));

        // Assert
        Assert.Equal(expectedOutcome, outcome);
        Assert.Same(expected, Assert.IsType<Failure<int>>(outcome).Exception);
        Assert.Equal(0, callbackCalls);
    }

    [Fact]
    public async Task Validate_Async_Description_Lowers_And_Defers_Both_Callbacks()
    {
        // Arrange
        var predicateCalls = 0;
        var factoryCalls = 0;
        Func<int, Task<bool>> predicate = async value =>
        {
            predicateCalls++;
            await Task.Yield();
            return value > 0;
        };
        Func<int, Exception> exceptionFactory = _ =>
        {
            factoryCalls++;
            return new InvalidOperationException("invalid");
        };
        var description = (((Flow.Succeed(42)).AsDescription()).Validate(predicate, exceptionFactory)).AsDescription();

        // Act
        var descriptionText = DescriptionFormatter.Describe(description);
        var lowered = description.Lower(FacadeBehaviourStrategyFactory.Instance);
        var predicateCallsBeforeExecution = predicateCalls;
        var factoryCallsBeforeExecution = factoryCalls;
        var outcome = await FlowEngine.ExecuteAsync(description);
        var expected = await FlowEngine.ExecuteAsync(
            Flow.Succeed(42).Validate(value => Task.FromResult(value > 0),
                _ => new InvalidOperationException("unused")));

        // Assert
        Assert.Equal(expected, outcome);
        Assert.Equal(Outcome.Success(42), outcome);
        Assert.IsType<Ast.Validate.Async<int>>(lowered);
        Assert.Contains("Validate.Async<int>", descriptionText);
        Assert.Contains("predicate: lambda(int) -> Task<bool>", descriptionText);
        Assert.Equal(0, predicateCallsBeforeExecution);
        Assert.Equal(0, factoryCallsBeforeExecution);
        Assert.Equal(1, predicateCalls);
        Assert.Equal(0, factoryCalls);
    }

    [Fact]
    public async Task Validate_CancellableAsync_Description_Passes_Execution_Token()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var observedToken = CancellationToken.None;
        Func<int, CancellationToken, Task<bool>> predicate = (value, token) =>
        {
            observedToken = token;
            return Task.FromResult(value > 0);
        };
        var description = (((Flow.Succeed(42)).AsDescription()).Validate(predicate, _ => new InvalidOperationException("unused"))).AsDescription();

        // Act
        var descriptionText = DescriptionFormatter.Describe(description);
        var outcome = await FlowEngine.ExecuteAsync(description, new Execution.Options(cancellationTokenSource.Token));
        var expected = await FlowEngine.ExecuteAsync(
            Flow.Succeed(42).Validate(
                (value, _) => Task.FromResult(value > 0),
                _ => new InvalidOperationException("unused")),
            new Execution.Options(cancellationTokenSource.Token));

        // Assert
        Assert.Equal(expected, outcome);
        Assert.Equal(Outcome.Success(42), outcome);
        Assert.Equal(cancellationTokenSource.Token, observedToken);
        Assert.Contains("Validate.CancellableAsync<int>", descriptionText);
        Assert.Contains("predicate: lambda(int, CancellationToken) -> Task<bool>", descriptionText);
        Assert.IsType<Ast.Validate.CancellableAsync<int>>(description.Lower(FacadeBehaviourStrategyFactory.Instance));
    }

    [Fact]
    public async Task Validate_CancellableAsync_Description_Skips_Predicate_When_Already_Cancelled()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        var predicateCalls = 0;
        Func<int, CancellationToken, Task<bool>> predicate = (_, _) =>
        {
            predicateCalls++;
            return Task.FromResult(true);
        };
        var description = (((Flow.Succeed(42)).AsDescription()).Validate(predicate, _ => new InvalidOperationException("unused"))).AsDescription();
        var options = new Execution.Options(cancellationTokenSource.Token);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(description, options);
        var expected = await FlowEngine.ExecuteAsync(
            Flow.Succeed(42).Validate(
                (_, _) => Task.FromResult(true),
                _ => new InvalidOperationException("unused")), options);

        // Assert
        Assert.IsType<TaskCanceledException>(Assert.IsType<Failure<int>>(expected).Exception);
        Assert.IsType<TaskCanceledException>(Assert.IsType<Failure<int>>(outcome).Exception);
        Assert.Equal(0, predicateCalls);
    }
}
