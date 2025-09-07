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
                await Task.Delay(1);
                return "async selected";
            }));
            Add(Flow.Succeed("s").DoOnSuccess(_ => { }));
            Add(Flow.Succeed("s").DoOnSuccess(async _ => await Task.Delay(1)));
            Add(Flow.Succeed("s").DoOnFailure(_ => { }));
            Add(Flow.Succeed("s").DoOnFailure(async _ => await Task.Delay(1)));
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
            await Task.Delay(TimeSpan.FromMilliseconds(80));
            return SimoneDeBeauvoir;
        });

        var timedFlow = flow.WithTimeout(TimeSpan.FromMilliseconds(20));

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
            await Task.Delay(TimeSpan.FromMilliseconds(5));
            return SimoneDeBeauvoir;
        });

        var timedFlow = flow.WithTimeout(TimeSpan.FromMilliseconds(50));

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
            await Task.Delay(TimeSpan.FromMilliseconds(50));
            return Flow.Succeed(SimoneDeBeauvoir);
        });

        var timedFlow = flow.WithTimeout(TimeSpan.FromMilliseconds(10));

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
            Thread.Sleep(TimeSpan.FromMilliseconds(80)); // Simulate long-running sync operation without blocking tasks
            return SimoneDeBeauvoir;
        });

        var timedFlow = flow.WithTimeout(TimeSpan.FromMilliseconds(20));

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
            await Task.Delay(TimeSpan.FromMilliseconds(20));
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
            await Task.Delay(TimeSpan.FromMilliseconds(80));
            return SimoneDeBeauvoir;
        });

        // Timeout is 20ms, so the first attempt will fail.
        // Since the default policy for WithRetry is to not retry on TimeoutException, the retry should NOT happen
        var resilientFlow = flow.WithTimeout(TimeSpan.FromMilliseconds(20)).WithRetry(3);

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
        var flow = Flow.Create<string>(async () =>
        {
            attempts++;
            // Each attempt takes 30ms
            await Task.Delay(TimeSpan.FromMilliseconds(30));
            // Always throw to force retries
            throw new InvalidOperationException($"Attempt {attempts} failed");
        });

        // Total timeout is 80ms, which should allow for ~2 full attempts (60ms) and possibly start a 3rd.
        var resilientFlow = flow.WithRetry(5)
                               .WithTimeout(TimeSpan.FromMilliseconds(80));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(resilientFlow);

        // Assert
        // Timeout does not cancel the underlying operation; extra attempts may continue in background.
        // Assert a sensible lower bound only to avoid flakiness across OS/schedulers.
        Assert.True(attempts >= 2);
        Assert.True(outcome.IsFailure());
        Assert.IsType<TimeoutException>(outcome switch { Failure<string> f => f.Exception, _ => null });
    }
}
