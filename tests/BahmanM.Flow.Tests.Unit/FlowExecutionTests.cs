using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow.Tests.Unit;

public class FlowExecutionTests
{
    [Fact]
    public async Task ExecuteAsync_WithSuccessfulFlow_ShouldReturnSuccessOutcome()
    {
        // Arrange
        var successfulFlow = Flow.Succeed(123);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(successfulFlow);

        // Assert
        Assert.Equal(Success(123), outcome);
    }

    [Fact]
    public async Task ExecuteAsync_WithFailedFlow_ShouldReturnFailureOutcome()
    {
        // Arrange
        var exception = new InvalidOperationException("Boom!");
        var failedFlow = Flow.Fail<int>(exception);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(failedFlow);

        // Assert
        Assert.Equal(Failure<int>(exception), outcome);
    }

    [Fact]
    public async Task ExecuteAsync_WithSuccessfulCreateOperation_ShouldReturnSuccessOutcome()
    {
        // Arrange
        var createFlow = Flow.Create(() => 123);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(createFlow);

        // Assert
        Assert.Equal(Success(123), outcome);
    }

    [Fact]
    public async Task ExecuteAsync_WithFailingCreateOperation_ShouldReturnFailureOutcome()
    {
        // Arrange
        var exception = new InvalidOperationException("Boom!");
        var createFlow = Flow.Create<int>(async () => throw exception);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(createFlow);

        // Assert
        Assert.Equal(Failure<int>(exception), outcome);
    }

    [Fact]
    public async Task ExecuteAsync_WithSuccessfulAsyncCreateOperation_ShouldReturnSuccessOutcome()
    {
        // Arrange
        var createFlow = Flow.Create(async () =>
        {
            await Task.Delay(10);
            return 123;
        });

        // Act
        var outcome = await FlowEngine.ExecuteAsync(createFlow);

        // Assert
        Assert.Equal(Success(123), outcome);
    }
}
