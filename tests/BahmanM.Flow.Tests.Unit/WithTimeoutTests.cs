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
        Assert.IsType<TimeoutException>(outcome.ExceptionOrDefault());
    }
}
