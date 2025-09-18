# Advanced Concepts

### Composable Behaviours

The real power of Flow is its plug-and-play design. Because a Flow is just a 'recipe', it can be enhanced with new behaviours without ever touching the original code.

For example, you can take an existing Flow and easily add resiliency policies like retries and timeouts:

```csharp
// Assume GetUserFlow is defined elsewhere and you can't modify it.
var originalFlow = GetUserFlow(123);

// At the call-site, you can enrich it with new behaviours.
var resilientFlow = originalFlow
    .WithRetry(3)
    .WithTimeout(TimeSpan.FromSeconds(5))
    .DoOnFailure(ex => 
        _logger.LogError(ex, "Ultimately failed to get user"));
```

### Complex Composition

Flow is designed to handle complex, real-world scenarios cleanly. Here is the core logic for a Kafka consumer that processes a message by composing multiple failable steps, including a partial recovery for one of the steps.

```csharp
var consumeFlow =
    Flow.Succeed(message)
        .Select(_adapters.AsOrderId)
        .Chain(orderId => 
            _orders.FindOrderFlow(orderId))
        .Validate(
            order => order is not null,
            _ => new NotFoundException("Order not found"))
        .Chain(order => 
            _rates
                .GetShippingRateFlow(order.ShipTo)
                .Recover(ex => 
                    ex is HttpNotFoundException 
                        ? Flow.Succeed(ShippingRate.StandardFallback)
                        : Flow.Fail<ShippingRate>(ex))
                .Select(rate => 
                    (order, rate)))
        .Select(x => 
            _adapters.AsDispatchMessage(x.order, x.rate))
        .Chain(dispatchMessage => 
            _producer.ProduceFlow(dispatchMessage));
```

### The Core Recipe

This recipe shows how to use Flow's resiliency and recovery operators to handle failures gracefully.

```csharp
// --- Adding Resiliency ---
var resilient = initialFlow
    .WithRetry(3)
    .WithTimeout(TimeSpan.FromSeconds(5));

// --- Handling Failures ---
var recovered = initialFlow.Recover(ex => GetFallbackFlow(ex));
```

---

### Next Up

Now that you have a solid high-level understanding of Flow, you are ready to dive into the details of the specific operators.

*   **[The Core Operators](../reference/core-operators.md)**
