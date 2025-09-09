using BahmanM.Flow.Behaviour;
using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow.Tests.Integration;

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

file class SpyBehaviour : IBehaviour
{
    public int ApplyCallCount { get; private set; }
    public string OperationType => "Test.Spy";

    public IFlow<T> Apply<T>(IFlow<T> originalFlow)
    {
        ApplyCallCount++;
        return originalFlow;
    }
}


public class WithBehaviourTests
{
    // Ada Lovelace (1815-1852) was an English mathematician and writer, chiefly known
    // for her work on Charles Babbage's proposed mechanical general-purpose computer,
    // the Analytical Engine.
    private const string AdaLovelace = "Ada Lovelace";

    public class AllNodeTypesTheoryData : TheoryData<IFlow<string>>
    {
        public AllNodeTypesTheoryData()
        {
            Add(Flow.Succeed("succeeded"));
            Add(Flow.Fail<string>(new Exception("dummy")));
            Add(Flow.Create(() => "created"));
            Add(Flow.Create<string>(async () =>
            {
                await Task.Yield();
                return "async created";
            }));
            Add(Flow.Succeed("s").DoOnSuccess(_ => { }));
            Add(Flow.Succeed("s").DoOnSuccess(async _ => await Task.Yield()));
            Add(Flow.Succeed("s").DoOnFailure(_ => { }));
            Add(Flow.Succeed("s").DoOnFailure(async _ => await Task.Yield()));
            Add(Flow.Succeed("s").Select(_ => "selected"));
            Add(Flow.Succeed("s").Select<string, string>(async _ =>
            {
                await Task.Yield();
                return "async selected";
            }));
            Add(Flow.Succeed("s").Chain(_ => Flow.Succeed<string>("chained")));
            Add(Flow.Succeed("s").Chain(async _ =>
            {
                await Task.Yield();
                return Flow.Succeed<string>("async chained");
            }));
        }
    }

    [Theory]
    [ClassData(typeof(AllNodeTypesTheoryData))]
    public void WithBehaviour_OnAnyNodeType_AppliesBehaviourExactlyOnce(IFlow<string> flow)
    {
        // Arrange
        var spy = new SpyBehaviour();

        // Act
        _ = flow.WithBehaviour(spy);

        // Assert
        Assert.Equal(1, spy.ApplyCallCount);
    }

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

    [Fact]
    public void WithBehaviour_WhenChained_AppliesEachBehaviourOnce()
    {
        // Arrange
        var spy1 = new SpyBehaviour();
        var spy2 = new SpyBehaviour();
        var flow = Flow.Create(() => "action");

        // Act
        _ = flow.WithBehaviour(spy1).WithBehaviour(spy2);

        // Assert
        // The first behaviour is applied to the CreateNode.
        // The second behaviour is applied to the result of the first application.
        Assert.Equal(1, spy1.ApplyCallCount);
        Assert.Equal(1, spy2.ApplyCallCount);
    }

    [Fact]
    public void WithBehaviour_WhenInterleaved_AppliesEachBehaviourOnceAtPointOfApplication()
    {
        // Arrange
        var spy1 = new SpyBehaviour();
        var spy2 = new SpyBehaviour();

        // Act
        _ = Flow.Create(() => 1)
            .WithBehaviour(spy1) // Applied once to the CreateNode
            .Select(i => i.ToString())
            .WithBehaviour(spy2); // Applied once to the SelectNode

        // Assert
        Assert.Equal(1, spy1.ApplyCallCount);
        Assert.Equal(1, spy2.ApplyCallCount);
    }
}
