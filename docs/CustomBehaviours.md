# Creating Custom Behaviours

So, you want to build your own operator? You're in the right place. This is an advanced topic, but it's where `Flow` becomes truly powerful.

### Operator vs. Behaviour

First, let's clarify some terms:

*   An **Operator** is a method you call in a `Flow` chain, like `.WithRetry()` or `.Select()`.
*   A **Behaviour** is the underlying logic that powers an operator. The `.WithRetry(3)` operator, for example, is powered by a built-in retry *behaviour*.

The `IBehaviour<T>` interface is your entry point for creating your own custom behaviours, which you can then apply to any `Flow` using the generic `.WithBehaviour()` operator.

### When Do You Need a Custom Behaviour?

You should create a custom behaviour when you have a **stateful, cross-cutting concern** that you want to apply to different `Flows` as a single, reusable unit. Good examples include:

*   A circuit breaker that tracks failure rates.
*   A complex logging mechanism that needs to maintain its own state.
*   A caching strategy with custom invalidation logic.

---

## Example: A Simple Circuit Breaker

Let's build a simple circuit breaker from scratch. Our goal: create a behaviour that will "trip" (stop executing `Flows`) after 3 consecutive failures.

### Step 1: The State

First, we need a simple class to hold the state of our circuit breaker. This object will be shared and managed by our application.

```csharp
public class CircuitBreakerState
{
    public int ConsecutiveFailures { get; private set; }

    public bool IsTripped(int failureThreshold) => ConsecutiveFailures >= failureThreshold;

    public void RecordFailure() => ConsecutiveFailures++;

    public void RecordSuccess() => ConsecutiveFailures = 0;
}
```

### Step 2: The Behaviour

Next, we implement the `IBehaviour<T>` interface. Our implementation will hold a reference to the state object and the failure threshold.

```csharp
public class CircuitBreakerBehaviour<T> : IBehaviour<T>
{
    private readonly CircuitBreakerState _state;
    private readonly int _failureThreshold;

    public CircuitBreakerBehaviour(CircuitBreakerState state, int failureThreshold = 3)
    {
        _state = state;
        _failureThreshold = failureThreshold;
    }

    // This gives our custom behaviour a unique name for diagnostics.
    public string OperationType => "Flow.CircuitBreaker";

    // This is where the magic happens.
    public IFlow<T> Apply(IFlow<T> originalFlow)
    {
        // 1. Check the state BEFORE doing anything.
        if (_state.IsTripped(_failureThreshold))
        {
            // If the circuit is open, immediately return a failed Flow.
            return Flow.Fail<T>(new Exception("Circuit breaker is open."));
        }

        // 2. If the circuit is closed, decorate the original Flow with our logic.
        return originalFlow
            .DoOnSuccess(_ => _state.RecordSuccess())      // On success, reset the counter.
            .DoOnFailure(_ => _state.RecordFailure());     // On failure, increment it.
    }
}
```

### Step 3: Using Your New Behaviour

Now you can use your custom behaviour with the generic `.WithBehaviour()` operator.

```csharp
// Create an instance of your behaviour, along with its state.
var circuitBreaker = new CircuitBreakerBehaviour<User>(new CircuitBreakerState());

// Now, apply it to any flow.
var resilientFlow = GetUserFromFlakyApiFlow(123)
    .WithBehaviour(circuitBreaker);

// When this flow is executed, the circuit breaker will do its job.
var outcome = await FlowEngine.ExecuteAsync(resilientFlow);
```

---

## What's Next?

That's it! You now know how to extend `Flow` with your own powerful, reusable behaviours.

From here, you have a few options:
*   Dive into the **[Design Rationale](./DesignRationale.md)** to better understand the "why" behind the library's architecture.
*   Browse the **[API Blueprint](./ApiBlueprint.cs)** to see all available methods and operators.
*   Head back to the **[Learning Path in the main README](../README.md#intrigued-heres-your-learning-path-🗺️)** to choose your next destination.