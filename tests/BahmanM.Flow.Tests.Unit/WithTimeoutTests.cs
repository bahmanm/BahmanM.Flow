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
}
