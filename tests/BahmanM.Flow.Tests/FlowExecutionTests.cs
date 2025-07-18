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
        Assert.Equal(new Success<int>(123), outcome);
    }
}
