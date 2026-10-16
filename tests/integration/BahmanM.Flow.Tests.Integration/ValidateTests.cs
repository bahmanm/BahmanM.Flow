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
            .Validate((async x => { await Task.Yield(); return x > 0; }), _ => new Exception("should not be called"));

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
            .Validate((async x => { await Task.Yield(); return x % 2 == 0; }), _ => expected);

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
            .Validate((async _ => { await Task.Yield(); return true; }), _ => new Exception("unused"));

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
            .Validate((async _ => { await Task.Yield(); throw predicateEx; }), _ => new Exception("unused"));

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
            .Validate((async x => { await Task.Yield(); return x % 2 == 0; }), x => new Exception($"{x} is odd"))
            .Recover(async _ =>
            {
                await Task.Yield();
                return Flow.Succeed(100);
            });

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Success(100), outcome);
    }

    [Fact]
    public async Task ValidateAsync_BehavioursNoOp_PreservesOutcome()
    {
        // Arrange
        var original = Flow.Succeed(10)
            .Validate((async x => { await Task.Yield(); return x > 0; }), _ => new Exception("bad"));

        // Act
        var withRetry = original.WithRetry(3);
        var withTimeout = original.WithTimeout(TimeSpan.FromMilliseconds(50));

        // Assert
        var expected = await FlowEngine.ExecuteAsync(original);
        Assert.Equal(expected, await FlowEngine.ExecuteAsync(withRetry));
        Assert.Equal(expected, await FlowEngine.ExecuteAsync(withTimeout));
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
            .Validate((Func<int, bool>)(_ => throw predicateEx), _ => new Exception("unused"));

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
            .Validate(_ => false, _ => throw factoryEx);

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
            .Recover((_ => Flow.Succeed(100)));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Success(100), outcome);
    }

    [Fact]
    public async Task Validate_WithRetry_IsNoOpAndPreservesOutcome()
    {
        // Arrange
        var original = Flow.Succeed(10)
            .Validate(x => x > 0, _ => new Exception("bad"));

        // Act
        var withRetry = original.WithRetry(3);

        // Assert
        Assert.Equal(await FlowEngine.ExecuteAsync(original), await FlowEngine.ExecuteAsync(withRetry));
    }

    [Fact]
    public async Task Validate_WithTimeout_IsNoOpAndPreservesOutcome()
    {
        // Arrange
        var original = Flow.Succeed(10)
            .Validate(x => x > 0, _ => new Exception("bad"));

        // Act
        var withTimeout = original.WithTimeout(TimeSpan.FromMilliseconds(50));

        // Assert
        Assert.Equal(await FlowEngine.ExecuteAsync(original), await FlowEngine.ExecuteAsync(withTimeout));
    }

    // Cancellable async Validate

    [Fact]
    public async Task ValidateCancellable_WhenSuccessAndPredicateTrue_PassesThroughSuccess()
    {
        // Arrange
        var flow = Flow.Succeed(42)
            .Validate(async (x, _) => { await Task.Yield(); return x > 0; }, _ => new Exception("should not be called"));

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
            .Validate(async (x, _) => { await Task.Yield(); return x % 2 == 0; }, _ => expected);

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
            .Validate(async (_, _) => { await Task.Yield(); return true; }, _ => new Exception("unused"));

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
            .Validate(async (x, _) => {
                ArgumentOutOfRangeException.ThrowIfNegative(x);
                await Task.Yield(); throw predicateEx; }, _ => new Exception("unused"));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Failure<int>(predicateEx), outcome);
    }

    [Fact]
    public async Task ValidateCancellable_BehavioursNoOp_PreservesOutcome()
    {
        // Arrange
        var original = Flow.Succeed(10)
            .Validate(async (x, _) => { await Task.Yield(); return x > 0; }, _ => new Exception("bad"));

        // Act
        var withRetry = original.WithRetry(3);
        var withTimeout = original.WithTimeout(TimeSpan.FromMilliseconds(50));

        // Assert
        var expected = await FlowEngine.ExecuteAsync(original);
        Assert.Equal(expected, await FlowEngine.ExecuteAsync(withRetry));
        Assert.Equal(expected, await FlowEngine.ExecuteAsync(withTimeout));
    }
}
