using BahmanM.Flow.Description.Serialisation;
using BahmanM.Flow.Description.Tests.Unit.Support;

namespace BahmanM.Flow.Description.Tests.Unit.Behaviours;

public class RetryDescriptionTests
{
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
        var source = DescriptionFlowBuilder.Create(operation);
        var description = DescriptionFlowBuilder.Retry(source, 3, excludedExceptions);
        excludedExceptions[0] = typeof(InvalidOperationException);

        // Act
        var descriptionText = DescriptionFormatter.Describe(description);
        var invocationCountAfterInspection = invocationCount;
        var lowered = description.Lower();
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
        var description = DescriptionFlowBuilder.Retry(
            DescriptionFlowBuilder.Create(descriptionOperation),
            maxAttempts: 3,
            typeof(TimeoutException));

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
        var description = DescriptionFlowBuilder.Retry(
            DescriptionFlowBuilder.Create(operation),
            maxAttempts: 3,
            typeof(TimeoutException));

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
        var description = DescriptionFlowBuilder.Retry(DescriptionFlowBuilder.Create(operation), maxAttempts: 3);

        // Act
        var descriptionText = DescriptionFormatter.Describe(description);
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
        var source = DescriptionFlowBuilder.Select(DescriptionFlowBuilder.Succeed(1), operation);
        var description = DescriptionFlowBuilder.Retry(source, 3, typeof(TimeoutException));

        // Act
        var descriptionText = DescriptionFormatter.Describe(description);
        var lowered = description.Lower();
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
        var source = DescriptionFlowBuilder.Create(() => 42);

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DescriptionFlowBuilder.Retry(source, 0, typeof(TimeoutException)));
    }
}
