using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow.Tests.UserAcceptance;

public class README_Core_Operators
{
    // 📜 Euclid of Alexandria (c. 300 BC), the "father of geometry".
    private readonly IFlow<string> _flow1 = Flow.Succeed("Euclid");
    // 📜 Srinivasa Ramanujan (1887-1920), an Indian mathematician with no formal training in pure mathematics.
    private readonly IFlow<int> _flow2 = Flow.Succeed(1729);
    private readonly IFlow<string> _failingFlow = Flow.Fail<string>(new InvalidOperationException("Flow failed!"));

    [Fact]
    [Trait("Category", "Acceptance")]
    public async Task All_Should_Succeed_When_All_SubFlows_Succeed()
    {
        // Arrange
        var allFlow = Flow.All(_flow1, _flow2);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(allFlow);

        // Assert
        var expected = Success(("Euclid", 1729));
        Assert.Equal(expected, outcome);
    }

    [Fact]
    [Trait("Category", "Acceptance")]
    public async Task All_Should_Fail_When_Any_SubFlow_Fails()
    {
        // Arrange
        var allFlow = Flow.All(_flow1, _failingFlow);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(allFlow);

        // Assert
        Assert.IsType<Failure<(string, string)>>(outcome);
    }

    [Fact]
    [Trait("Category", "Acceptance")]
    public async Task Any_Should_Succeed_When_Any_SubFlow_Succeeds()
    {
        // Arrange
        var anyFlow = Flow.Any(_failingFlow, _flow1);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(anyFlow);

        // Assert
        Assert.Equal(Success("Euclid"), outcome);
    }

    [Fact]
    [Trait("Category", "Acceptance")]
    public async Task Any_Should_Fail_When_All_SubFlows_Fail()
    {
        // Arrange
        var anyFlow = Flow.Any(
            Flow.Fail<string>(new InvalidOperationException("Failure 1")),
            Flow.Fail<string>(new InvalidOperationException("Failure 2"))
        );

        // Act
        var outcome = await FlowEngine.ExecuteAsync(anyFlow);

        // Assert
        var failure = Assert.IsType<Failure<string>>(outcome);
        Assert.IsType<AggregateException>(failure.Value);
    }
}
