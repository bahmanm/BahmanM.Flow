using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow.Tests.Unit;

// Test fixture implementing the IBehaviour interface, as described in docs/Behaviours.md
file class CircuitBreakerBehaviour(CircuitBreakerState state, int failureThreshold = 3) : IBehaviour
{
    public string OperationType => "Test.CircuitBreaker";

    public IFlow<T> Apply<T>(IFlow<T> originalFlow)
    {
        if (state.IsTripped(failureThreshold))
        {
            return Flow.Fail<T>(new Exception("Circuit breaker is open."));
        }

        return originalFlow
            .DoOnSuccess(_ => state.RecordSuccess())
            .DoOnFailure(_ => state.RecordFailure());
    }
}

file class CircuitBreakerState
{
    public int ConsecutiveFailures { get; private set; }
    public bool IsTripped(int failureThreshold) => ConsecutiveFailures >= failureThreshold;
    public void RecordFailure() => ConsecutiveFailures++;
    public void RecordSuccess() => ConsecutiveFailures = 0;
}


public class WithBehaviourTests
{
    // Ada Lovelace (1815-1852) was an English mathematician and writer, chiefly known
    // for her work on Charles Babbage's proposed mechanical general-purpose computer,
    // the Analytical Engine.
    private const string AdaLovelace = "Ada Lovelace";

    [Fact]
    public async Task WithBehaviour_WhenFlowSucceeds_ExecutesSuccessPathSideEffectFromBehaviour()
    {
        // Arrange
        var state = new CircuitBreakerState();
        var circuitBreaker = new CircuitBreakerBehaviour(state);
        var flow = Flow.Create(() => AdaLovelace);

        // Act
        var resilientFlow = flow.WithBehaviour(circuitBreaker);
        var outcome = await FlowEngine.ExecuteAsync(resilientFlow);

        // Assert
        Assert.Equal(Success(AdaLovelace), outcome);
        Assert.Equal(0, state.ConsecutiveFailures); // Success was recorded
    }

    [Fact]
    public async Task WithBehaviour_WhenFlowFails_ExecutesFailurePathSideEffectFromBehaviour()
    {
        // Arrange
        var state = new CircuitBreakerState();
        var circuitBreaker = new CircuitBreakerBehaviour(state);
        var exception = new InvalidOperationException("Test failure");
        var flow = Flow.Fail<string>(exception);

        // Act
        var resilientFlow = flow.WithBehaviour(circuitBreaker);
        var outcome = await FlowEngine.ExecuteAsync(resilientFlow);

        // Assert
        Assert.Equal(Failure<string>(exception), outcome);
        Assert.Equal(1, state.ConsecutiveFailures); // Failure was recorded
    }
}
