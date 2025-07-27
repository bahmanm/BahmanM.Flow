using BahmanM.Flow.Execution;
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
        var flow = Flow.Succeed(123).Select((Operations.Select.Sync<int, int>)((_) => throw exception));

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
        var flow = Flow.Succeed(successValue).Select((Operations.Select.Async<int, int>)(async x =>
        {
            await Task.Delay(10);
            return x * 2;
        }));

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

    [Fact]
    public async Task WhenCancellableAsyncFlowSucceeds_AppliesSelectorAndReturnsNewSuccess()
    {
        // Arrange
        var successValue = 123;
        var flow = Flow.Succeed(successValue).Select(async (x, token) =>
        {
            await Task.Delay(10, token);
            return x * 2;
        });

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Success(246), outcome);
    }

    [Fact]
    public async Task WhenCancellableAsyncSelectorIsCancelled_ReturnsFailure()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var options = new FlowExecutionOptions { CancellationToken = cts.Token };

        var flow = Flow.Succeed(123).Select(async (x, token) =>
        {
            await Task.Delay(100, token);
            return x * 2;
        });

        // Act
        await cts.CancelAsync();
        var outcome = await FlowEngine.ExecuteAsync(flow, options);

        // Assert
        var failure = Assert.IsType<Failure<int>>(outcome);
        Assert.IsType<TaskCanceledException>(failure.Exception);
        
        // Clean up
        cts.Dispose();
    }
}
