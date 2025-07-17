# Creating Custom Behaviours

The Flow library is designed to be extensible. 

While it comes with built-in behaviours like `.WithRetry()` and `.WithTimeout()`, you can create your own complex, stateful operators by implementing the `IBehaviour<T>` interface.

This is an advanced feature, but it allows you to encapsulate complex logic into a reusable operator.

## Example: A Simple Circuit Breaker

Here is a conceptual example of how you could implement a circuit breaker. A real-world implementation would be more robust, but this demonstrates the pattern.

The `WithBehaviour` operator allows for the application of custom, stateful behaviours that can alter the control flow.

```csharp
// The user defines a reusable behaviour by implementing the IBehaviour<T> interface.
public class CircuitBreakerBehaviour<T> : IBehaviour<T>
{
    // The behaviour provides its own type for richer diagnostics.
    public string OperationType => "Flow.CircuitBreaker";

    // The Apply method returns a new flow that wraps the original.
    public IFlow<T> Apply(IFlow<T> originalFlow)
    {
        // The implementation would check the circuit breaker's state.
        // If the circuit is open, it would return Flow.Fail(...).
        // Otherwise, it would execute the originalFlow and update the state.
        return Flow.Create(() => {
            // ... state-checking and wrapping logic would go here ...
            return FlowEngine.ExecuteAsync(originalFlow);
        });
    }
}

// The user can then apply this behaviour to any flow.
var circuitState = new CircuitBreakerState();
var resilientFlow = CreateFlakyApiServiceCall()
    .WithBehaviour(new CircuitBreakerBehaviour(circuitState));
```
