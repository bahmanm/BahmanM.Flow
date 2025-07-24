using System;
using System.Threading.Tasks;
using BahmanM.Flow;
using Xunit;
using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow.Tests.Unit;

public class WithTimeoutTests
{
    // Simone de Beauvoir (1908–1986) was a French writer, intellectual, existentialist philosopher,
    // political activist, feminist, and social theorist.
    private const string SimoneDeBeauvoir = "Simone de Beauvoir";

    [Fact]
    public async Task WithTimeout_WhenOperationExceedsDuration_FailsWithTimeoutException()
    {
        // Arrange
        var flow = Flow.Create(async () =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(200));
            return SimoneDeBeauvoir;
        });

        var timedFlow = flow.WithTimeout(TimeSpan.FromMilliseconds(50));

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
        var flow = Flow.Create(async () =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(50));
            return SimoneDeBeauvoir;
        });

        var timedFlow = flow.WithTimeout(TimeSpan.FromMilliseconds(200));

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
            await Task.Delay(TimeSpan.FromMilliseconds(200));
            return Flow.Succeed(SimoneDeBeauvoir);
        });

        var timedFlow = flow.WithTimeout(TimeSpan.FromMilliseconds(50));

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
        var flow = Flow.Create(() =>
        {
            System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(200));
            return SimoneDeBeauvoir;
        });

        var timedFlow = flow.WithTimeout(TimeSpan.FromMilliseconds(50));

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
        var flow = Flow.Create(async () =>
        {
            attempts++;
            await Task.Delay(TimeSpan.FromMilliseconds(40));
            if (attempts < 3)
            {
                throw new InvalidOperationException("Flaky");
            }
            return SimoneDeBeauvoir;
        });

        // Total time will be ~120ms (3 * 40ms). Timeout is 200ms.
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
        var flow = Flow.Create(async () =>
        {
            attempts += 1;
            await Task.Delay(TimeSpan.FromMilliseconds(200));
            return SimoneDeBeauvoir;
        });

        // Timeout is 50ms, so the first attempt will fail.
        // Since the default policy for WithRetry is to not retry on TimeoutException, the retry should NOT happen
        var resilientFlow = flow.WithTimeout(TimeSpan.FromMilliseconds(50)).WithRetry(3);

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
            // Each attempt takes 75ms
            await Task.Delay(TimeSpan.FromMilliseconds(75));
            // Always throw to force retries
            throw new InvalidOperationException($"Attempt {attempts} failed");
        });

        // Total timeout is 200ms, which should allow for 2 full attempts (150ms) but not 3 (225ms)
        var resilientFlow = flow.WithRetry(5) // Max 5 retries, but timeout should occur first
                               .WithTimeout(TimeSpan.FromMilliseconds(200));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(resilientFlow);

        // Assert
        // Should make 2 or 3 attempts depending on timing (3rd might start but not finish)
        Assert.InRange(attempts, 2, 3);
        Assert.True(outcome.IsFailure());
        Assert.IsType<TimeoutException>(outcome switch { Failure<string> f => f.Exception, _ => null });
    }
}
