using BahmanM.Flow;
using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow.Tests.Unit;

public class SelectTests
{
    [Fact]
    public async Task WhenFlowSucceeds_AppliesSelectorAndReturnsNewSuccess()
    {
        // Arrange
        var successValue = 123;
        var flow = Flow.Succeed(successValue).Select(x => x * 2);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Success(246), outcome);
    }

    [Fact]
    public async Task WhenFlowFails_DoesNotApplySelectorAndReturnsOriginalFailure()
    {
        // Arrange
        var exception = new InvalidOperationException("Test Failure");
        var flow = Flow.Fail<int>(exception).Select(x => x * 2);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Failure<int>(exception), outcome);
    }

    [Fact]
    public async Task WhenSelectorThrows_ReturnsFailure()
    {
        // Arrange
        var exception = new InvalidOperationException("Selector failed!");
        var flow = Flow.Succeed(123).Select((Func<int, int>)(_ => throw exception));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Failure<int>(exception), outcome);
    }

    [Fact]
    public async Task WhenAsyncFlowSucceeds_AppliesAsyncSelectorAndReturnsNewSuccess()
    {
        // Arrange
        var successValue = 123;
        var flow = Flow.Succeed(successValue).Select(async x =>
        {
            await Task.Delay(10);
            return x * 2;
        });

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Success(246), outcome);
    }

    [Fact]
    public async Task WhenAsyncSelectorThrows_ReturnsFailure()
    {
        // Arrange
        var exception = new InvalidOperationException("Async selector failed!");
        var flow = Flow.Succeed(123).Select<int, int>(async _ =>
        {
            await Task.Delay(10);
            throw exception;
        });

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Failure<int>(exception), outcome);
    }
}
