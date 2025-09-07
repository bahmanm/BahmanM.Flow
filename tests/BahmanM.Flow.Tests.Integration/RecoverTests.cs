using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow.Tests.Integration;

public class RecoverTests
{
    [Fact]
    public async Task Recover_OnSuccessfulFlow_DoesNotExecuteAndReturnsOriginalValue()
    {
        // Arrange
        var initialFlow = Flow.Succeed(10);
        var recoverExecuted = false;
        var recoveredFlow = initialFlow.Recover((Flow.Operations.Recover.Sync<int>)(ex =>
        {
            recoverExecuted = true;
            return Flow.Succeed(0);
        }));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(recoveredFlow);

        // Assert
        Assert.False(recoverExecuted);
        Assert.Equal(Success(10), outcome);
    }

    [Fact]
    public async Task Recover_OnFailedFlow_ExecutesAndReturnsNewFlow()
    {
        // Arrange
        var exception = new Exception("Initial failure");
        var initialFlow = Flow.Fail<int>(exception);
        var recoveredFlow = initialFlow.Recover((Flow.Operations.Recover.Sync<int>) (ex => Flow.Succeed(20)));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(recoveredFlow);

        // Assert
        Assert.Equal(Success(20), outcome);
    }

    [Fact]
    public async Task WhenRecoverFunctionThrows_ReturnsFailure()
    {
        // Arrange
        var initialException = new Exception("Initial failure");
        var recoverException = new InvalidOperationException("Recover function failed!");
        var initialFlow = Flow.Fail<int>(initialException);
        var recoveredFlow = initialFlow.Recover((Flow.Operations.Recover.Sync<int>)(_ => throw recoverException));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(recoveredFlow);

        // Assert
        Assert.Equal(Failure<int>(recoverException), outcome);
    }

    [Fact]
    public async Task AsyncRecover_OnSuccessfulFlow_DoesNotExecuteAndReturnsOriginalValue()
    {
        // Arrange
        var initialFlow = Flow.Succeed(10);
        var recoverExecuted = false;
        var recoveredFlow = initialFlow.Recover((Flow.Operations.Recover.Async<int>)(async _ =>
        {
            recoverExecuted = true;
            await Task.Yield();
            return Flow.Succeed(0);
        }));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(recoveredFlow);

        // Assert
        Assert.False(recoverExecuted);
        Assert.Equal(Success(10), outcome);
    }

    [Fact]
    public async Task AsyncRecover_OnFailedFlow_ExecutesAndReturnsNewFlow()
    {
        // Arrange
        var exception = new Exception("Initial failure");
        var initialFlow = Flow.Fail<int>(exception);
        var recoveredFlow = initialFlow.Recover((Flow.Operations.Recover.Async<int>)(async ex =>
        {
            await Task.Yield();
            return Flow.Succeed(20);
        }));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(recoveredFlow);

        // Assert
        Assert.Equal(Success(20), outcome);
    }

    [Fact]
    public async Task WhenAsyncRecoverFunctionThrows_ReturnsFailure()
    {
        // Arrange
        var initialException = new Exception("Initial failure");
        var recoverException = new InvalidOperationException("Async recover function failed!");
        var initialFlow = Flow.Fail<int>(initialException);
        var recoveredFlow = initialFlow.Recover((Flow.Operations.Recover.Async<int>)(_ => throw recoverException));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(recoveredFlow);

        // Assert
        Assert.Equal(Failure<int>(recoverException), outcome);
    }

    [Fact]
    public async Task CancellableAsyncRecover_OnSuccessfulFlow_DoesNotExecuteAndReturnsOriginalValue()
    {
        // Arrange
        var initialFlow = Flow.Succeed(10);
        var recoverExecuted = false;
        var recoveredFlow = initialFlow.Recover(async (ex, token) =>
        {
            recoverExecuted = true;
            await Task.Yield();
            return Flow.Succeed(0);
        });

        // Act
        var outcome = await FlowEngine.ExecuteAsync(recoveredFlow);

        // Assert
        Assert.False(recoverExecuted);
        Assert.Equal(Success(10), outcome);
    }

    [Fact]
    public async Task WhenCancellableAsyncRecoverIsCancelled_ReturnsFailure()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var options = new Execution.Options(CancellationToken: cts.Token);
        var initialException = new Exception("Initial failure");

        var recoveredFlow = Flow.Fail<int>(initialException).Recover(async (ex, token) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return Flow.Succeed(20);
        });

        // Act
        await cts.CancelAsync();
        var outcome = await FlowEngine.ExecuteAsync(recoveredFlow, options);

        // Assert
        var failure = Assert.IsType<Failure<int>>(outcome);
        Assert.IsType<TaskCanceledException>(failure.Exception);

        // Clean up
        cts.Dispose();
    }

    // New test cases

    [Fact]
    public async Task Recover_AfterRetryExhausted_ExecutesAndReturnsNewFlow()
    {
        // Arrange
        var initialFlow = Flow.Create((Func<int>)(() => throw new InvalidOperationException("Always fails")))
                                .WithRetry(1); // Only one attempt, so it will fail
        var recoveredFlow = initialFlow.Recover((Flow.Operations.Recover.Sync<int>)(ex => Flow.Succeed(99)));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(recoveredFlow);

        // Assert
        Assert.Equal(Success(99), outcome);
    }

    [Fact]
    public async Task Recover_AfterTimeout_ExecutesAndReturnsNewFlow()
    {
        // Arrange
        var initialFlow = Flow.Create(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(2)); // Will timeout
            return 10;
        }).WithTimeout(TimeSpan.FromMilliseconds(20));
        var recoveredFlow = initialFlow.Recover(_ => Task.FromResult(Flow.Succeed(88)));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(recoveredFlow);

        // Assert
        Assert.Equal(Success(88), outcome);
    }

    [Fact]
    public async Task NestedRecover_InnerRecovers_OuterDoesNotExecute()
    {
        // Arrange
        var innerRecoverExecuted = false;
        var outerRecoverExecuted = false;

        var flow = Flow.Fail<int>(new Exception("Initial failure"))
                       .Recover((Flow.Operations.Recover.Sync<int>)(ex =>
                       {
                           innerRecoverExecuted = true;
                           return Flow.Succeed(100);
                       }))
                       .Recover((Flow.Operations.Recover.Sync<int>)(ex =>
                       {
                           outerRecoverExecuted = true;
                           return Flow.Succeed(200);
                       }));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.True(innerRecoverExecuted);
        Assert.False(outerRecoverExecuted);
        Assert.Equal(Success(100), outcome);
    }

    [Fact]
    public async Task NestedRecover_InnerFailsToRecover_OuterExecutes()
    {
        // Arrange
        var innerRecoverExecuted = false;
        var outerRecoverExecuted = false;

        var flow = Flow.Fail<int>(new Exception("Initial failure"))
                       .Recover((Flow.Operations.Recover.Sync<int>)(ex =>
                       {
                           innerRecoverExecuted = true;
                           throw new InvalidOperationException("Inner recover failed");
                       }))
                       .Recover((Flow.Operations.Recover.Sync<int>)(ex =>
                       {
                           outerRecoverExecuted = true;
                           return Flow.Succeed(200);
                       }));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.True(innerRecoverExecuted);
        Assert.True(outerRecoverExecuted);
        Assert.Equal(Success(200), outcome);
    }

    [Fact]
    public async Task Recover_WithSpecificExceptionType_OnlyHandlesMatchingException()
    {
        // Arrange
        var customExceptionHandled = false;
        var flow = Flow.Fail<int>(new CustomTestException("Custom failure"))
                       .Recover((Flow.Operations.Recover.Sync<int>)(ex =>
                       {
                           if (ex is CustomTestException)
                           {
                               customExceptionHandled = true;
                               return Flow.Succeed(10);
                           }
                           throw ex; // Re-throw other exceptions
                       }));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.True(customExceptionHandled);
        Assert.Equal(Success(10), outcome);
    }

    [Fact]
    public async Task Recover_WithSpecificExceptionType_PropagatesNonMatchingException()
    {
        // Arrange
        var customExceptionHandled = false;
        var nonMatchingException = new InvalidOperationException("Non-matching failure");
        var flow = Flow.Fail<int>(nonMatchingException)
                       .Recover((Flow.Operations.Recover.Sync<int>)(ex =>
                       {
                           if (ex is CustomTestException)
                           {
                               customExceptionHandled = true;
                               return Flow.Succeed(10);
                           }
                           throw ex; // Re-throw other exceptions
                       }));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.False(customExceptionHandled);
        var failure = Assert.IsType<Failure<int>>(outcome);
        Assert.Same(nonMatchingException, failure.Exception);
    }

    [Fact]
    public async Task Recover_ReturnsFailFlow_PropagatesFailure()
    {
        // Arrange
        var originalException = new Exception("Original failure");
        var recoveredException = new InvalidOperationException("Recovered failure");
        var flow = Flow.Fail<int>(originalException)
                       .Recover((Flow.Operations.Recover.Sync<int>)(ex => Flow.Fail<int>(recoveredException)));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        var failure = Assert.IsType<Failure<int>>(outcome);
        Assert.Same(recoveredException, failure.Exception);
    }

    // Helper exception for testing specific exception types
    public class CustomTestException : Exception
    {
        public CustomTestException(string message) : base(message) { }
    }
}
