using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow.Tests.Integration;

public class ValidateTests
{
    [Fact]
    public async Task Validate_WhenSuccessAndPredicateTrue_PassesThroughSuccess()
    {
        // Arrange
        var flow = Flow.Succeed(42)
            .Validate(x => x > 0, _ => new Exception("should not be called"));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Success(42), outcome);
    }

    [Fact]
    public async Task ValidateAsync_WhenSuccessAndPredicateTrue_PassesThroughSuccess()
    {
        // Arrange
        var flow = Flow.Succeed(42)
            .Validate((Func<int, Task<bool>>)(async x => { await Task.Delay(1); return x > 0; }), _ => new Exception("should not be called"));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Success(42), outcome);
    }

    [Fact]
    public async Task ValidateAsync_WhenSuccessAndPredicateFalse_FailsWithFactoryException()
    {
        // Arrange
        var expected = new InvalidOperationException("invalid value");
        var flow = Flow.Succeed(5)
            .Validate((Func<int, Task<bool>>)(async x => { await Task.Delay(1); return x % 2 == 0; }), _ => expected);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Failure<int>(expected), outcome);
    }

    [Fact]
    public async Task ValidateAsync_WhenUpstreamIsFailure_PassesThroughFailure()
    {
        // Arrange
        var upstreamEx = new Exception("upstream failure");
        var flow = Flow.Fail<int>(upstreamEx)
            .Validate((Func<int, Task<bool>>)(async _ => { await Task.Delay(1); return true; }), _ => new Exception("unused"));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Failure<int>(upstreamEx), outcome);
    }

    [Fact]
    public async Task ValidateAsync_WhenPredicateThrows_ReturnsFailure()
    {
        // Arrange
        var predicateEx = new InvalidOperationException("predicate crashed");
        var flow = Flow.Succeed(1)
            .Validate((Func<int, Task<bool>>)(async x => { await Task.Delay(1); throw predicateEx; }), _ => new Exception("unused"));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Failure<int>(predicateEx), outcome);
    }

    [Fact]
    public async Task ValidateAsync_WithRecover_ActsAsIfElseAtFlowLevel()
    {
        // Arrange
        var flow = Flow.Succeed(7)
            .Validate((Func<int, Task<bool>>)(async x => { await Task.Delay(1); return x % 2 == 0; }), x => new Exception($"{x} is odd"))
            .Recover((Flow.Operations.Recover.Async<int>)(async _ =>
            {
                await Task.Delay(1);
                return Flow.Succeed(100);
            }));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Success(100), outcome);
    }

    [Fact]
    public void ValidateAsync_BehavioursNoOp_ReturnsSameInstance()
    {
        // Arrange
        var original = Flow.Succeed(10)
            .Validate((Func<int, Task<bool>>)(async x => { await Task.Delay(1); return x > 0; }), _ => new Exception("bad"));

        // Act
        var withRetry = original.WithRetry(3);
        var withTimeout = original.WithTimeout(TimeSpan.FromMilliseconds(50));

        // Assert
        Assert.Equal(original, withRetry);
        Assert.Equal(original, withTimeout);
    }

    [Fact]
    public async Task Validate_WhenSuccessAndPredicateFalse_FailsWithFactoryException()
    {
        // Arrange
        var expected = new InvalidOperationException("invalid value");
        var flow = Flow.Succeed(5)
            .Validate(x => x % 2 == 0, _ => expected);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Failure<int>(expected), outcome);
    }

    [Fact]
    public async Task Validate_WhenUpstreamIsFailure_PassesThroughFailure()
    {
        // Arrange
        var upstreamEx = new Exception("upstream failure");
        var flow = Flow.Fail<int>(upstreamEx)
            .Validate(_ => true, _ => new Exception("unused"));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Failure<int>(upstreamEx), outcome);
    }

    [Fact]
    public async Task Validate_WhenPredicateThrows_ReturnsFailureWithThrownException()
    {
        // Arrange
        var predicateEx = new InvalidOperationException("predicate crashed");
        var flow = Flow.Succeed(1)
            .Validate((Func<int, bool>)(x => throw predicateEx), _ => new Exception("unused"));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Failure<int>(predicateEx), outcome);
    }

    [Fact]
    public async Task Validate_WhenExceptionFactoryThrows_ReturnsFailureWithThrownException()
    {
        // Arrange
        var factoryEx = new InvalidOperationException("factory crashed");
        var flow = Flow.Succeed(1)
            .Validate(x => false, _ => throw factoryEx);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Failure<int>(factoryEx), outcome);
    }

    [Fact]
    public async Task Validate_WithRecover_ActsAsIfElseAtFlowLevel()
    {
        // Arrange
        var flow = Flow.Succeed(7)
            .Validate(x => x % 2 == 0, x => new Exception($"{x} is odd"))
            .Recover((Flow.Operations.Recover.Sync<int>)(_ => Flow.Succeed(100)));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Success(100), outcome);
    }

    [Fact]
    public void Validate_WithRetry_IsNoOpAndReturnsSameInstance()
    {
        // Arrange
        var original = Flow.Succeed(10)
            .Validate(x => x > 0, _ => new Exception("bad"));

        // Act
        var withRetry = original.WithRetry(3);

        // Assert
        Assert.Equal(original, withRetry);
    }

    [Fact]
    public void Validate_WithTimeout_IsNoOpAndReturnsSameInstance()
    {
        // Arrange
        var original = Flow.Succeed(10)
            .Validate(x => x > 0, _ => new Exception("bad"));

        // Act
        var withTimeout = original.WithTimeout(TimeSpan.FromMilliseconds(50));

        // Assert
        Assert.Equal(original, withTimeout);
    }

    // Cancellable async Validate

    [Fact]
    public async Task ValidateCancellable_WhenSuccessAndPredicateTrue_PassesThroughSuccess()
    {
        // Arrange
        var flow = Flow.Succeed(42)
            .Validate(async (x, token) => { await Task.Delay(1, token); return x > 0; }, _ => new Exception("should not be called"));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Success(42), outcome);
    }

    [Fact]
    public async Task ValidateCancellable_WhenCancelled_ReturnsTaskCanceledFailure()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var options = new Execution.Options(CancellationToken: cts.Token);
        var flow = Flow.Succeed(42)
            .Validate(async (x, token) => { await Task.Delay(100, token); return x > 0; }, _ => new Exception("unused"));

        // Act
        await cts.CancelAsync();
        var outcome = await FlowEngine.ExecuteAsync(flow, options);

        // Assert
        var failure = Assert.IsType<Failure<int>>(outcome);
        Assert.IsType<TaskCanceledException>(failure.Exception);

        // Clean up
        cts.Dispose();
    }

    [Fact]
    public async Task ValidateCancellable_WhenSuccessAndPredicateFalse_FailsWithFactoryException()
    {
        // Arrange
        var expected = new InvalidOperationException("invalid value");
        var flow = Flow.Succeed(5)
            .Validate(async (x, token) => { await Task.Delay(1, token); return x % 2 == 0; }, _ => expected);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Failure<int>(expected), outcome);
    }

    [Fact]
    public async Task ValidateCancellable_WhenUpstreamIsFailure_PassesThroughFailure()
    {
        // Arrange
        var upstreamEx = new Exception("upstream failure");
        var flow = Flow.Fail<int>(upstreamEx)
            .Validate(async (_, token) => { await Task.Delay(1, token); return true; }, _ => new Exception("unused"));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Failure<int>(upstreamEx), outcome);
    }

    [Fact]
    public async Task ValidateCancellable_WhenPredicateThrows_ReturnsFailure()
    {
        // Arrange
        var predicateEx = new InvalidOperationException("predicate crashed");
        var flow = Flow.Succeed(1)
            .Validate<int>(async (x, token) => { await Task.Delay(1, token); throw predicateEx; }, _ => new Exception("unused"));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Failure<int>(predicateEx), outcome);
    }

    [Fact]
    public void ValidateCancellable_BehavioursNoOp_ReturnsSameInstance()
    {
        // Arrange
        var original = Flow.Succeed(10)
            .Validate(async (x, token) => { await Task.Delay(1, token); return x > 0; }, _ => new Exception("bad"));

        // Act
        var withRetry = original.WithRetry(3);
        var withTimeout = original.WithTimeout(TimeSpan.FromMilliseconds(50));

        // Assert
        Assert.Equal(original, withRetry);
        Assert.Equal(original, withTimeout);
    }
}
