using Xunit;

namespace BahmanM.Flow.Tests;

public class FlowExecutionTests
{
    [Fact]
    public void Execute_WithSuccessfulFlow_ShouldReturnSuccessOutcome()
    {
        // Arrange
        var successfulFlow = Flow.Succeed(123);

        // Act
        var outcome = FlowEngine.Execute(successfulFlow);

        // Assert
        Assert.Equal(Outcome.Success(123), outcome);
    }

    [Fact]
    public void Execute_WithFailedFlow_ShouldReturnFailureOutcome()
    {
        // Arrange
        var exception = new InvalidOperationException("Boom!");
        var failedFlow = Flow.Fail<int>(exception);

        // Act
        var outcome = FlowEngine.Execute(failedFlow);

        // Assert
        Assert.Equal(Outcome.Failure<int>(exception), outcome);
    }

    [Fact]
    public void Execute_WithSuccessfulCreateOperation_ShouldReturnSuccessOutcome()
    {
        // Arrange
        var createFlow = Flow.Create(() => 123);

        // Act
        var outcome = FlowEngine.Execute(createFlow);

        // Assert
        Assert.Equal(Outcome.Success(123), outcome);
    }

    [Fact]
    public void Execute_WithFailingCreateOperation_ShouldReturnFailureOutcome()
    {
        // Arrange
        var exception = new InvalidOperationException("Boom!");
        var createFlow = Flow.Create<int>(() => throw exception);

        // Act
        var outcome = FlowEngine.Execute(createFlow);

        // Assert
        Assert.Equal(Outcome.Failure<int>(exception), outcome);
    }
}
