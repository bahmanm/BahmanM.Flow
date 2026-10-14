using BahmanM.Flow.Description.Serialisation;
using BahmanM.Flow.Description.Tests.Unit.Support;

namespace BahmanM.Flow.Description.Tests.Unit;

public class DescriptionTests
{
    [Fact]
    public async Task Succeed_Description_Lowers_To_Equivalent_Outcome()
    {
        // Arrange
        var description = DescriptionFlowBuilder.Succeed("Hello");
        var expectedFlow = Flow.Succeed("Hello");

        // Act
        var actual = await FlowEngine.ExecuteAsync(description);
        var expected = await FlowEngine.ExecuteAsync(expectedFlow);

        // Assert
        Assert.Equal(expected, actual);
        Assert.IsType<Ast.Primitive.Succeed<string>>(description.Lower());
    }

    [Fact]
    public async Task Fail_Description_Preserves_Exception_Identity()
    {
        // Arrange
        var exception = new InvalidOperationException("failure");
        var description = DescriptionFlowBuilder.Fail<string>(exception);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(description);

        // Assert
        Assert.Same(exception, Assert.IsType<Failure<string>>(outcome).Exception);
        Assert.IsType<Ast.Primitive.Fail<string>>(description.Lower());
    }

    [Fact]
    public async Task Create_Description_Defers_Operation_Until_Execution()
    {
        // Arrange
        var invocationCount = 0;
        Flow.Operations.Create.Sync<int> operation = () => ++invocationCount;
        var description = DescriptionFlowBuilder.Create(operation);

        // Act
        var invocationCountBeforeExecution = invocationCount;
        var outcome = await FlowEngine.ExecuteAsync(description);

        // Assert
        Assert.Equal(0, invocationCountBeforeExecution);
        Assert.Equal(1, Assert.IsType<Success<int>>(outcome).Value);
        Assert.Equal(1, invocationCount);
        Assert.IsType<Ast.Create.Sync<int>>(description.Lower());
    }

    [Fact]
    public async Task Create_Async_Description_Defers_Operation_Until_Execution()
    {
        // Arrange
        var invocationCount = 0;
        Flow.Operations.Create.Async<int> operation = () => Task.FromResult(++invocationCount);
        var description = DescriptionFlowBuilder.Create(operation);

        // Act
        var invocationCountBeforeExecution = invocationCount;
        var outcome = await FlowEngine.ExecuteAsync(description);

        // Assert
        Assert.Equal(0, invocationCountBeforeExecution);
        Assert.Equal(1, Assert.IsType<Success<int>>(outcome).Value);
        Assert.Equal(1, invocationCount);
        Assert.IsType<Ast.Create.Async<int>>(description.Lower());
    }

    [Fact]
    public async Task Create_Async_Description_Captures_Operation_Exception()
    {
        // Arrange
        var exception = new InvalidOperationException("async failure");
        Flow.Operations.Create.Async<int> operation = () => Task.FromException<int>(exception);
        var description = DescriptionFlowBuilder.Create(operation);

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
        var description = DescriptionFlowBuilder.Create(operation);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(description, new Execution.Options(cancellationTokenSource.Token));

        // Assert
        Assert.Equal(cancellationTokenSource.Token, observedToken);
        Assert.Equal(1, Assert.IsType<Success<int>>(outcome).Value);
        Assert.IsType<Ast.Create.CancellableAsync<int>>(description.Lower());
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
        var description = DescriptionFlowBuilder.Create(operation);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(description, new Execution.Options(cancellationTokenSource.Token));

        // Assert
        Assert.IsAssignableFrom<OperationCanceledException>(Assert.IsType<Failure<int>>(outcome).Exception);
        Assert.Equal(0, invocationCount);
    }

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
        var description = DescriptionFlowBuilder.Select(DescriptionFlowBuilder.Succeed("Hello"), operation);
        var expectedFlow = Flow.Succeed("Hello").Select(value => value.Length);

        // Act
        var invocationCountBeforeExecution = invocationCount;
        var actual = await FlowEngine.ExecuteAsync(description);
        var expected = await FlowEngine.ExecuteAsync(expectedFlow);

        // Assert
        Assert.Equal(0, invocationCountBeforeExecution);
        Assert.Equal(expected, actual);
        Assert.Equal(1, invocationCount);
        Assert.IsType<Ast.Select.Sync<string, int>>(description.Lower());
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
        var description = DescriptionFlowBuilder.Select(DescriptionFlowBuilder.Succeed("Hello"), operation);
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
        Assert.IsType<Ast.Select.Async<string, int>>(description.Lower());
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
        var description = DescriptionFlowBuilder.Select(DescriptionFlowBuilder.Succeed("Hello"), operation);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(description, new Execution.Options(cancellationTokenSource.Token));

        // Assert
        Assert.Equal(cancellationTokenSource.Token, observedToken);
        Assert.Equal(5, Assert.IsType<Success<int>>(outcome).Value);
        Assert.IsType<Ast.Select.CancellableAsync<string, int>>(description.Lower());
    }

    [Fact]
    public void Description_Identity_Is_Stable_And_Hash_Derived()
    {
        // Arrange
        var description = DescriptionFlowBuilder.Succeed("Hello");

        // Act
        var expectedShortId = Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(
            System.Text.Encoding.ASCII.GetBytes(description.Id)))[..10].ToLowerInvariant();
        var descriptionText = DescriptionFormatter.Describe(description);

        // Assert
        Assert.Equal(32, description.Id.Length);
        Assert.All(description.Id, character => Assert.Contains(character, "0123456789abcdef"));
        Assert.Contains($"[node: {expectedShortId}]", descriptionText);
    }

    [Fact]
    public void Describe_Shows_Structure_And_Callback_Identity_Without_Invoking_Callback()
    {
        // Arrange
        var invocationCount = 0;
        Flow.Operations.Select.Sync<string, int> operation = value =>
        {
            invocationCount++;
            return value.Length;
        };
        var description = DescriptionFlowBuilder.Select(DescriptionFlowBuilder.Succeed("Hello"), operation);

        // Act
        var result = DescriptionFormatter.Describe(description);

        // Assert
        Assert.Contains("Select.Sync<string, int>", result);
        Assert.Contains("operation: lambda(string) -> int", result);
        Assert.Contains("Succeed<string>", result);
        Assert.Contains("value: \"Hello\"", result);
        Assert.Equal(0, invocationCount);
    }

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
        var description = DescriptionFlowBuilder.Validate(
            DescriptionFlowBuilder.Succeed(42), predicate, exceptionFactory);

        // Act
        _ = DescriptionFormatter.Describe(description);
        var lowered = description.Lower();
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
        var description = DescriptionFlowBuilder.Validate(
            DescriptionFlowBuilder.Succeed(0), _ => false, _ => expected);

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
        Assert.IsType<Ast.Validate.Sync<int>>(description.Lower());
    }

    [Fact]
    public async Task Validate_Sync_Description_Passes_Through_Upstream_Failure_Without_Calling_Callbacks()
    {
        // Arrange
        var expected = new InvalidOperationException("upstream");
        var callbackCalls = 0;
        var description = DescriptionFlowBuilder.Validate(
            DescriptionFlowBuilder.Fail<int>(expected),
            _ => { callbackCalls++; return true; },
            _ => { callbackCalls++; return new Exception("unused"); });

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
    public async Task DoOnSuccess_Sync_Description_Runs_Action_And_Preserves_Success()
    {
        // Arrange
        var actionCalls = 0;
        Flow.Operations.DoOnSuccess.Sync<int> action = value =>
        {
            Assert.Equal(42, value);
            actionCalls++;
        };
        var description = DescriptionFlowBuilder.DoOnSuccess(DescriptionFlowBuilder.Succeed(42), action);

        // Act
        var descriptionText = DescriptionFormatter.Describe(description);
        var callsBeforeExecution = actionCalls;
        var lowered = description.Lower();
        var outcome = await FlowEngine.ExecuteAsync(description);
        var expectedOutcome = await FlowEngine.ExecuteAsync(
            Flow.Succeed(42).DoOnSuccess((Flow.Operations.DoOnSuccess.Sync<int>)(_ => { })));

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
        var description = DescriptionFlowBuilder.DoOnSuccess(DescriptionFlowBuilder.Succeed(42), action);

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
        var description = DescriptionFlowBuilder.DoOnSuccess(DescriptionFlowBuilder.Fail<int>(expected), action);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(description);
        var expectedOutcome = await FlowEngine.ExecuteAsync(
            Flow.Fail<int>(expected).DoOnSuccess((Flow.Operations.DoOnSuccess.Sync<int>)(_ => { })));

        // Assert
        Assert.Equal(expectedOutcome, outcome);
        Assert.Same(expected, Assert.IsType<Failure<int>>(outcome).Exception);
        Assert.Equal(0, actionCalls);
    }

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
            Flow.Fail<int>(expected).DoOnFailure((Flow.Operations.DoOnFailure.Sync)(_ => { })));

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
            Flow.Succeed(42).DoOnFailure((Flow.Operations.DoOnFailure.Sync)(_ => { })));

        // Assert
        Assert.Equal(expectedOutcome, outcome);
        Assert.Equal(Outcome.Success(42), outcome);
        Assert.Equal(0, actionCalls);
    }
}
