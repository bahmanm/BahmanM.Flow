/*
using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow.Tests.UserAcceptance;

public class README_Behavior_Enrichment
{
    [Fact]
    [Trait("Category", "Acceptance")]
    public async Task Should_Succeed_When_Operation_Succeeds_Within_Retry_Limit()
    {
        // Arrange
        var flakyService = new FlakyService(succeedOnAttempt: 3);
        var resilientFlow = flakyService.GetValueFlow()
            .WithRetry(maxRetries: 3);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(resilientFlow);

        // Assert
        Assert.Equal(Success("Success!"), outcome);
        Assert.Equal(3, flakyService.CallCount);
    }

    [Fact]
    [Trait("Category", "Acceptance")]
    public async Task Should_Fail_When_Operation_Keeps_Failing_After_All_Retries()
    {
        // Arrange
        var flakyService = new FlakyService(succeedOnAttempt: 5);
        var resilientFlow = flakyService.GetValueFlow()
            .WithRetry(maxRetries: 3);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(resilientFlow);

        // Assert
        Assert.IsType<Failure<string>>(outcome);
        Assert.Equal(4, flakyService.CallCount); // 1 initial call + 3 retries
    }

    [Fact]
    [Trait("Category", "Acceptance")]
    public async Task Should_Fail_With_Timeout_When_Operation_Is_Too_Slow()
    {
        // Arrange
        var slowService = new SlowService(delay: TimeSpan.FromMilliseconds(100));
        var timeoutFlow = slowService.GetValueFlow()
            .WithTimeout(TimeSpan.FromMilliseconds(20));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(timeoutFlow);

        // Assert
        var failure = Assert.IsType<Failure<string>>(outcome);
        Assert.IsType<TimeoutException>(failure.Value);
    }
}

#region Mocks and Stubs

internal class FlakyService(int succeedOnAttempt)
{
    public int CallCount { get; private set; }

    public IFlow<string> GetValueFlow()
    {
        return Flow.Create(() =>
        {
            CallCount++;
            if (CallCount < succeedOnAttempt)
            {
                throw new InvalidOperationException($"Failed on attempt {CallCount}");
            }
            return "Success!";
        });
    }
}

internal class SlowService(TimeSpan delay)
{
    public IFlow<string> GetValueFlow()
    {
        return Flow.Create(async () =>
        {
            await Task.Delay(delay);
            return "Success!";
        });
    }
}

#endregion
*/
