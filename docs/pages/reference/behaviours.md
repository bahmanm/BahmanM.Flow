# Behaviours

You've mastered the core operators, but have you ever wondered how `.WithRetry()` works under the hood? 

Welcome to the **Behaviour** system.

This is where you level up from being a user of Flow to a creator, forging your own reusable superpowers to extend Flow.

# Operator vs. Behaviour

To get started, let's clarify two key terms:

*   An **Operator** is one of the foundational, verb-based primitives you use to build a Flow operation-by-operation (e.g., `Select`, `Chain`, `Recover`). 

_Each operator is concerned with a single, specific part of the Flow._

*   A **Behaviour** is a higher-level, cross-cutting policy that enriches a `Flow` with a new capability. 

_Behaviours are applied with operators that start with `With` (e.g., `.WithRetry`, `.WithTimeout`) to signify that you are creating a new Flow *with* an added superpower._

## Applicability

The effect of `.WithRetry()` and `.WithTimeout()` depends on the operator immediately before them. Some common cases are:

| Applied directly to | `.WithRetry()` | `.WithTimeout()` |
| --- | --- | --- |
| `Flow.Create(...)` | Retries the creation callback. | Runs the creation callback in a timed scope. |
| `Select`, `Validate`, `DoOnSuccess`, or `DoOnFailure` | No effect; does not retry the callback or its upstream flow. | Applies timeout to the upstream flow; does not time the operator's own callback. |
| `Flow.WithResource(...)` | No effect; does not retry acquisition, use, or the returned child flow. | Places resource execution in a timed scope, subject to the limits below. |

These rules cover synchronous, asynchronous, and cancellable asynchronous variants. To retry a `Create` operation followed by `Select`, put `.WithRetry()` before `.Select()`.

Resource timeouts use cooperative cancellation. Synchronous acquisition can delay the start of the timer, and disposal may continue after a timeout result has been returned. A timeout therefore does not guarantee that all resource work has stopped when the result arrives.

Retrying a whole resource scope with fresh acquisition on each attempt is not currently implemented. Applying retry to a supported operation inside the resource scope retries that operation while retaining the same resource.

The generic `.WithBehaviour()` operator can apply custom logic, including logging or auditing, to any operator.

This entire system is designed for extensibility: The `IBehaviour` interface is your entry point for building any custom behaviour you can imagine, which you can then apply using the generic `.WithBehaviour()` operator.

# When Do You Need a Custom Behaviour?

You should create a custom behaviour when you have a **stateful, cross-cutting concern** that you want to apply to different Flows as a single, reusable unit.

Good examples include:

*   A circuit breaker that tracks failure rates.
*   A complex logging mechanism that needs to maintain its own state.
*   A caching strategy with custom invalidation logic.


# Example: Count Failures Across Executions

A custom behaviour can attach callbacks that update shared state as a flow executes. Here, a thread-safe counter records each failed execution.

```csharp
public sealed class FailureCounter
{
    private int _failures;

    public int Failures => Volatile.Read(ref _failures);

    public void RecordFailure() => Interlocked.Increment(ref _failures);
}

public sealed class FailureCountingBehaviour(FailureCounter counter) : IBehaviour
{
    public string OperationType => "Flow.FailureCounter";

    public IFlow<T> Apply<T>(IFlow<T> originalFlow) =>
        originalFlow.DoOnFailure(_ => counter.RecordFailure());
}
```

Apply the behaviour once, then execute the resulting flow. The attached callback updates the counter on each failed execution:

```csharp
var counter = new FailureCounter();
var observedFlow = GetUserFromApiFlow(123)
    .WithBehaviour(new FailureCountingBehaviour(counter));

await FlowEngine.ExecuteAsync(observedFlow);
await FlowEngine.ExecuteAsync(observedFlow);

Console.WriteLine($"Failures: {counter.Failures}");
```

`WithBehaviour` calls `Apply` when the flow is composed and retains its result. Put per-execution work in the returned flow's operators; a state check performed directly in `Apply` will not run again on later executions of the composed flow.

---

# What's Next?

That's it! You now know how to extend `Flow` with your own powerful, reusable behaviours.

From here, you have a few options:
*   Dive into the **[Design Rationale](./design-rationale.md)** to better understand the "why" behind Flow's architecture.
*   Browse the **[API Blueprint](./api-blueprint.md)** to see all available methods and operators.
*   Head back to the **[Learning Path in the main README](../README.md#intrigued-heres-your-learning-path-🗺️)** to choose your next destination.

---

## Next Steps

You've seen how to build custom behaviours. To see built-in behaviours like `WithRetry` and `WithTimeout` used in a complete, runnable application, explore the example project:

*   ["Should I Go Outside?" Example Application](https://github.com/bahmanm/BahmanM.Flow/tree/main/examples/ShouldIGoOutside)
