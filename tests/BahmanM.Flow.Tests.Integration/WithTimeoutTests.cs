namespace BahmanM.Flow.Tests.Integration;

public class WithTimeoutTests
{
    // Simone de Beauvoir (1908–1986) was a French writer, intellectual, existentialist philosopher,
    // political activist, feminist, and social theorist.
    private const string SimoneDeBeauvoir = "Simone de Beauvoir";

    public class NonFailableFlowsTheoryData : TheoryData<IFlow<string>>
    {
        public NonFailableFlowsTheoryData()
        {
            Add(Flow.Succeed("succeeded"));
            Add(Flow.Fail<string>(new Exception("dummy")));
            Add(Flow.Succeed("s").Select(_ => "selected"));
            Add(Flow.Succeed("s").Select<string,string>(async _ =>
            {
                await Task.Yield();
                return "async selected";
            }));
            Add(Flow.Succeed("s").DoOnSuccess(_ => { }));
            Add(Flow.Succeed("s").DoOnSuccess(async _ => await Task.Yield()));
            Add(Flow.Succeed("s").DoOnFailure(_ => { }));
            Add(Flow.Succeed("s").DoOnFailure(async _ => await Task.Yield()));
        }
    }


    [Theory]
    [ClassData(typeof(NonFailableFlowsTheoryData))]
    public void WithTimeout_OnNonFailableNodes_IsANoOp(IFlow<string> nonFailableFlow)
    {
        // Arrange
        var originalFlow = nonFailableFlow;
        var timeout = TimeSpan.FromMilliseconds(50);

        // Act
        var resultFlow = originalFlow.WithTimeout(timeout);

        // Assert
        Assert.Equal(originalFlow, resultFlow);
    }

    [Fact]
    public async Task WithTimeout_WhenOperationExceedsDuration_FailsWithTimeoutException()
    {
        // Arrange
        var flow = Flow.Create<string>(async () =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan);
            return SimoneDeBeauvoir;
        });

        var timedFlow = flow.WithTimeout(TimeSpan.Zero);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(timedFlow);

        // Assert
        Assert.True(outcome.IsFailure());
        var exception = outcome switch
        {
            Failure<string> f => f.Exception,
            _ => null
        };
        Assert.IsType<TimeoutException>(exception);
    }

    [Fact]
    public async Task WithTimeout_WhenOperationCompletesWithinDuration_Succeeds()
    {
        // Arrange
        var flow = Flow.Create<string>(async () =>
        {
            await Task.Yield();
            return SimoneDeBeauvoir;
        });

        var timedFlow = flow.WithTimeout(TimeSpan.FromSeconds(1));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(timedFlow);

        // Assert
        Assert.True(outcome.IsSuccess());
        Assert.Equal(SimoneDeBeauvoir, outcome.GetOrElse("fallback"));
    }

    [Fact]
    public async Task WithTimeout_WhenAsyncChainExceedsDuration_FailsWithTimeoutException()
    {
        // Arrange
        var flow = Flow.Succeed("start").Chain(async _ =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan);
            return Flow.Succeed(SimoneDeBeauvoir);
        });

        var timedFlow = flow.WithTimeout(TimeSpan.Zero);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(timedFlow);

        // Assert
        Assert.True(outcome.IsFailure());
        Assert.IsType<TimeoutException>(outcome switch { Failure<string> f => f.Exception, _ => null });
    }

    [Fact]
    public async Task WithTimeout_WhenSyncOperationExceedsDuration_FailsWithTimeoutException()
    {
        // Arrange
        var flow = Flow.Create<string>(() =>
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(80));
            return SimoneDeBeauvoir;
        });

        var timedFlow = flow.WithTimeout(TimeSpan.Zero);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(timedFlow);

        // Assert
        Assert.True(outcome.IsFailure());
        Assert.IsType<TimeoutException>(outcome switch { Failure<string> f => f.Exception, _ => null });
    }

    [Fact]
    public async Task WithRetryThenWithTimeout_WhenRetriesSucceedWithinTimeout_Succeeds()
    {
        // Arrange
        var attempts = 0;
        var flow = Flow.Create<string>(async () =>
        {
            attempts++;
            await Task.Yield();
            if (attempts < 3)
            {
                throw new InvalidOperationException("Flaky");
            }
            return SimoneDeBeauvoir;
        });

        // Total time will be ~60ms (3 * 20ms). Timeout is 200ms.
        var resilientFlow = flow.WithRetry(3).WithTimeout(TimeSpan.FromMilliseconds(200));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(resilientFlow);

        // Assert
        Assert.Equal(3, attempts);
        Assert.True(outcome.IsSuccess());
    }

    [Fact]
    public async Task WithTimeoutThenWithRetry_WhenFirstAttemptTimesOut_DoesNotRetry()
    {
        // Arrange
        var attempts = 0;
        var flow = Flow.Create<string>(async () =>
        {
            attempts += 1;
            await Task.Delay(Timeout.InfiniteTimeSpan);
            return SimoneDeBeauvoir;
        });

        // Timeout is 20ms, so the first attempt will fail.
        // Since the default policy for WithRetry is to not retry on TimeoutException, the retry should NOT happen
        var resilientFlow = flow.WithTimeout(TimeSpan.Zero).WithRetry(3);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(resilientFlow);

        // Assert
        Assert.Equal(1, attempts);
        Assert.True(outcome.IsFailure());
        Assert.IsType<TimeoutException>(outcome switch { Failure<string> f => f.Exception, _ => null });
    }

    [Fact]
    public async Task WithRetryThenWithTimeout_WhenRetriesExceedTimeout_FailsWithTimeoutException()
    {
        // Arrange
        var attempts = 0;
        var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var flow = Flow.Create<string>(async () =>
        {
            attempts++;
            if (attempts == 1)
            {
                await Task.Yield();
                throw new InvalidOperationException("Attempt 1 failed");
            }
            if (attempts == 2)
            {
                secondStarted.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan);
                throw new InvalidOperationException("unreachable");
            }
            await Task.Yield();
            throw new InvalidOperationException($"Attempt {attempts} failed");
        });

        var resilientFlow = flow.WithRetry(5)
                               .WithTimeout(TimeSpan.FromMilliseconds(50));

        // Act
        var task = FlowEngine.ExecuteAsync(resilientFlow);
        await secondStarted.Task;
        var outcome = await task;

        // Assert
        Assert.True(attempts >= 2);
        Assert.True(outcome.IsFailure());
        Assert.IsType<TimeoutException>(outcome switch { Failure<string> f => f.Exception, _ => null });
    }
}
