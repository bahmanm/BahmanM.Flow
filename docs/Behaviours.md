# Behaviours

You've mastered the core operators, but have you ever wondered how `.WithRetry()` works under the hood? 

Welcome to the **Behaviour** system.

This is where you level up from being a user of Flow to a creator, forging your own reusable superpowers to extend the library.

# Operator vs. Behaviour

To get started, let's clarify two key terms:

*   An **Operator** is one of the foundational, verb-based primitives you use to build a pipeline operation-by-operation (e.g., `Select`, `Chain`, `Recover`). 

_Each operator is concerned with a single, specific part of the Flow._

*   A **Behaviour** is a higher-level, cross-cutting policy that enriches a `Flow` with a new capability. 

_Behaviours are applied with operators that start with `With` (e.g., `.WithRetry`, `.WithTimeout`) to signify that you are creating a new Flow *with* an added superpower._

This entire system is designed for extensibility: The `IBehaviour<T>` interface is your entry point for building any custom behaviour you can imagine, which you can then apply using the generic `.WithBehaviour()` operator.

# When Do You Need a Custom Behaviour?

You should create a custom behaviour when you have a **stateful, cross-cutting concern** that you want to apply to different Flows as a single, reusable unit.

Good examples include:

*   A circuit breaker that tracks failure rates.
*   A complex logging mechanism that needs to maintain its own state.
*   A caching strategy with custom invalidation logic.


# Example: A Simple Circuit Breaker

Let's build a simple circuit breaker from scratch. Our goal: create a behaviour that will "trip" (stop executing a Flow) after 3 consecutive failures.

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

Next, we implement the `IBehaviour<T>` interface.

```csharp
public class CircuitBreakerBehaviour<T>(CircuitBreakerState state, int failureThreshold = 3) : IBehaviour<T>
{
    // This gives our custom behaviour a unique name for diagnostics.
    public string OperationType => "Flow.CircuitBreaker";

    // This is where the magic happens.
    public IFlow<T> Apply(IFlow<T> originalFlow)
    {
        // 1. Check the state BEFORE doing anything.
        if (state.IsTripped(failureThreshold))
        {
            // If the circuit is open, immediately return a failed Flow.
            return Flow.Fail<T>(new Exception("Circuit breaker is open."));
        }

        // 2. If the circuit is closed, decorate the original Flow with our logic.
        return originalFlow
            .DoOnSuccess(_ => state.RecordSuccess())      // On success, reset the counter.
            .DoOnFailure(_ => state.RecordFailure());     // On failure, increment it.
    }
}
```

### Step 3: Using Your New Behaviour

Now you can plug your custom behaviour into any Flow with the generic `.WithBehaviour()` operator.

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

# What's Next?

That's it! You now know how to extend `Flow` with your own powerful, reusable behaviours.

From here, you have a few options:
*   Dive into the **[Design Rationale](./DesignRationale.md)** to better understand the "why" behind the library's architecture.
*   Browse the **[API Blueprint](./ApiBlueprint.cs)** to see all available methods and operators.
*   Head back to the **[Learning Path in the main README](../README.md#intrigued-heres-your-learning-path-🗺️)** to choose your next destination.
