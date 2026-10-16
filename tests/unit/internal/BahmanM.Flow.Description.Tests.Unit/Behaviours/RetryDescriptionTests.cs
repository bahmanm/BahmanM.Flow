using BahmanM.Flow.Description.Rendering;

namespace BahmanM.Flow.Description.Tests.Unit.Behaviours;

public class RetryDescriptionTests
{
    [Fact]
    public async Task Retry_Description_RetriesAsyncCreate()
    {
        var attempts = 0;
        Flow.Operations.Create.Async<int> operation = async () =>
        {
            await Task.Yield();
            if (++attempts < 3)
                throw new InvalidOperationException("try again");
            return 42;
        };
        var description = (((Flow.Create(() => operation())).AsDescription()).WithRetry(maxAttempts: 3, typeof(TimeoutException))).AsDescription();

        var lowered = description.Lower(FacadeBehaviourStrategyFactory.Instance);
        Assert.Equal(0, attempts);
        Assert.IsType<Ast.Create.Async<int>>(lowered);

        var outcome = await FlowEngine.ExecuteAsync(description);

        Assert.Equal(Outcome.Success(42), outcome);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task Retry_Description_RetriesCancellableAsyncCreateAndPreservesToken()
    {
        var attempts = 0;
        var receivedTokens = new List<CancellationToken>();
        Flow.Operations.Create.CancellableAsync<int> operation = async token =>
        {
            receivedTokens.Add(token);
            await Task.Yield();
            if (++attempts < 3)
                throw new InvalidOperationException("try again");
            return 42;
        };
        var description = (((Flow.Create(cancellationToken => operation(cancellationToken))).AsDescription()).WithRetry(maxAttempts: 3, typeof(TimeoutException))).AsDescription();

        var lowered = description.Lower(FacadeBehaviourStrategyFactory.Instance);
        Assert.Equal(0, attempts);
        Assert.IsType<Ast.Create.CancellableAsync<int>>(lowered);

        var outcome = await FlowEngine.ExecuteAsync(description);

        Assert.Equal(Outcome.Success(42), outcome);
        Assert.Equal(3, attempts);
        Assert.All(receivedTokens, token => Assert.Equal(CancellationToken.None, token));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Retry_Description_RetainsApplicationAroundPrimitiveNoOp(bool fail)
    {
        var exception = new InvalidOperationException("primitive failure");
        var source = fail
            ? (Flow.Fail<int>(exception)).AsDescription()
            : (Flow.Succeed(42)).AsDescription();
        var description = ((source).WithRetry(maxAttempts: 3)).AsDescription();

        var text = Renderer.Describe(description);
        var lowered = description.Lower(FacadeBehaviourStrategyFactory.Instance);
        var outcome = await FlowEngine.ExecuteAsync(description);

        Assert.Contains("Retry<int>", text);
        Assert.IsType(fail ? typeof(Ast.Primitive.Fail<int>) : typeof(Ast.Primitive.Succeed<int>), lowered);
        if (fail)
            Assert.Same(exception, Assert.IsType<Failure<int>>(outcome).Exception);
        else
            Assert.Equal(Outcome.Success(42), outcome);
    }

    [Fact]
    public async Task Retry_Description_RetainsPolicy_AndRetriesCreateOnExecution()
    {
        // Arrange
        var invocationCount = 0;
        Flow.Operations.Create.Sync<int> operation = () =>
        {
            invocationCount++;
            if (invocationCount < 3)
                throw new InvalidOperationException("try again");
            return 42;
        };
        var excludedExceptions = new[] { typeof(TimeoutException) };
        var source = (Flow.Create(() => operation())).AsDescription();
        var description = ((source).WithRetry(3, excludedExceptions)).AsDescription();
        excludedExceptions[0] = typeof(InvalidOperationException);

        // Act
        var descriptionText = Renderer.Describe(description);
        var invocationCountAfterInspection = invocationCount;
        var lowered = description.Lower(FacadeBehaviourStrategyFactory.Instance);
        var invocationCountAfterLowering = invocationCount;
        var outcome = await FlowEngine.ExecuteAsync(description);

        // Assert
        Assert.NotEqual(source.Id, description.Id);
        Assert.Contains("Retry<int>", descriptionText);
        Assert.Contains("maxAttempts: 3", descriptionText);
        Assert.Contains("nonRetryableExceptions: TimeoutException", descriptionText);
        Assert.Contains("Create.Sync<int>", descriptionText);
        Assert.Equal(0, invocationCountAfterInspection);
        Assert.Equal(0, invocationCountAfterLowering);
        Assert.IsType<Ast.Create.Sync<int>>(lowered);
        Assert.Equal(42, Assert.IsType<Success<int>>(outcome).Value);
        Assert.Equal(3, invocationCount);
    }

    [Fact]
    public async Task Retry_Description_MatchesExistingRetryExecution()
    {
        // Arrange
        var descriptionInvocationCount = 0;
        Flow.Operations.Create.Sync<int> descriptionOperation = () =>
        {
            if (++descriptionInvocationCount < 3)
                throw new InvalidOperationException("try again");
            return 42;
        };
        var description = (((Flow.Create(() => descriptionOperation())).AsDescription()).WithRetry(maxAttempts: 3, typeof(TimeoutException))).AsDescription();

        var existingInvocationCount = 0;
        Flow.Operations.Create.Sync<int> existingOperation = () =>
        {
            if (++existingInvocationCount < 3)
                throw new InvalidOperationException("try again");
            return 42;
        };
        var existingFlow = Flow.Create(() => existingOperation()).WithRetry(3);

        // Act
        var descriptionOutcome = await FlowEngine.ExecuteAsync(description);
        var existingOutcome = await FlowEngine.ExecuteAsync(existingFlow);

        // Assert
        Assert.Equal(existingOutcome, descriptionOutcome);
        Assert.Equal(existingInvocationCount, descriptionInvocationCount);
        Assert.Equal(3, descriptionInvocationCount);
    }

    [Fact]
    public async Task Retry_Description_PreservesFinalExceptionIdentity()
    {
        // Arrange
        var exception = new InvalidOperationException("still failing");
        var invocationCount = 0;
        Flow.Operations.Create.Sync<int> operation = () =>
        {
            invocationCount++;
            throw exception;
        };
        var description = (((Flow.Create(() => operation())).AsDescription()).WithRetry(maxAttempts: 3, typeof(TimeoutException))).AsDescription();

        // Act
        var outcome = await FlowEngine.ExecuteAsync(description);

        // Assert
        Assert.Same(exception, Assert.IsType<Failure<int>>(outcome).Exception);
        Assert.Equal(3, invocationCount);
    }

    [Fact]
    public async Task Retry_Description_DefaultPolicyDoesNotRetryTimeoutException()
    {
        // Arrange
        var exception = new TimeoutException("timed out");
        var invocationCount = 0;
        Flow.Operations.Create.Sync<int> operation = () =>
        {
            invocationCount++;
            throw exception;
        };
        var description = (((Flow.Create(() => operation())).AsDescription()).WithRetry(maxAttempts: 3)).AsDescription();

        // Act
        var descriptionText = Renderer.Describe(description);
        var outcome = await FlowEngine.ExecuteAsync(description);

        // Assert
        Assert.Contains("nonRetryableExceptions: TimeoutException", descriptionText);
        Assert.Same(exception, Assert.IsType<Failure<int>>(outcome).Exception);
        Assert.Equal(1, invocationCount);
    }

    [Fact]
    public async Task Retry_Description_RetainsApplication_WhenSelectStrategyIsNoOp()
    {
        // Arrange
        var invocationCount = 0;
        Flow.Operations.Select.Sync<int, int> operation = value =>
        {
            invocationCount++;
            throw new InvalidOperationException($"cannot select {value}");
        };
        var source = (((Flow.Succeed(1)).AsDescription()).Select(operation)).AsDescription();
        var description = ((source).WithRetry(3, typeof(TimeoutException))).AsDescription();

        // Act
        var descriptionText = Renderer.Describe(description);
        var lowered = description.Lower(FacadeBehaviourStrategyFactory.Instance);
        var outcome = await FlowEngine.ExecuteAsync(description);

        // Assert
        Assert.Contains("Retry<int>", descriptionText);
        Assert.Contains("Select.Sync<int, int>", descriptionText);
        Assert.IsType<Ast.Select.Sync<int, int>>(lowered);
        Assert.IsType<Failure<int>>(outcome);
        Assert.Equal(1, invocationCount);
    }

    [Fact]
    public void Retry_Description_RejectsNonPositiveAttemptCountAtConstruction()
    {
        // Arrange
        var source = (Flow.Create(() => 42)).AsDescription();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ((source).WithRetry(0, typeof(TimeoutException))).AsDescription());
    }
}
