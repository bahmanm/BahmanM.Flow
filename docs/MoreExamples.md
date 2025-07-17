# Key Scenarios & Usage

This section provides client-side code snippets for each major operator, which can serve as a basis for user acceptance tests.

## Core Operations

### 1. Declaring and Executing Flows

```csharp
// A simple synchronous flow
var simpleFlow = Flow.Succeed(42).Select(x => x * 2);
var result = await FlowEngine.ExecuteAsync(simpleFlow);

// A flow with asynchronous operations
var asyncFlow = Flow.Succeed("user-123", new FlowId("user-enrichment"))
    .Chain(userId => GetUserFromDatabaseAsync(userId));
var userResult = await FlowEngine.ExecuteAsync(asyncFlow);
```

### 2. Transformation (`Select`)

```csharp
// Synchronous transformation
var syncSelect = Flow.Succeed("  hello  ").Select(text => text.Trim());

// Asynchronous transformation
var asyncSelect = Flow.Succeed(42)
                      .Select(async userId => await GetUserNameFromDbAsync(userId));
```

### 3. Sequencing (`Chain`)

```csharp
// Chaining a synchronous operation that returns an Outcome
var syncChain = Flow.Succeed(5).Chain(value => Outcome.Success(value + 10));

// Chaining an asynchronous operation that returns a Task<Outcome>
var asyncChain = Flow.Succeed(42)
                     .Chain(async userId => await GetUserDataAsync(userId));
```

### 4. Failure Recovery (`Recover`)

```csharp
// Recovering with a simple fallback value
var simpleRecovery = Flow.Fail<string>(new Exception("...")).Recover("Default Value");

// Recovering with an asynchronous function
var asyncRecovery = Flow.Fail<User>(new Exception("..."))
                        .Recover(async ex => await GetDefaultUserFromCacheAsync(ex));
```

### 5. Performing Side Effects (`DoOnSuccess` / `DoOnFailure`)

```csharp
// Synchronous side effect
var syncTap = Flow.Succeed(42).DoOnSuccess(v => Console.WriteLine(v));

// Asynchronous side effect
var asyncTap = Flow.Succeed("user-123")
                     .DoOnSuccess(async id => await _auditService.LogAccessAsync(id));
```

## Advanced Operations

### 1. Operation-Scoped Resources (`Flow.WithResource`)

```csharp
var resourceFlow = Flow.Succeed("user-123")
    .Chain(userId =>
        Flow.WithResource(
            acquire: () => new HttpClient(),
            use: httpClient => NotifyServiceAsync(httpClient, userId)
        )
    );
```

### 2. Concurrency (`Flow.All` & `Flow.Any`)

```csharp
// Flow.All runs all flows and returns an array of results.
var allFlow = Flow.All(
    GetUserAsync(1),
    GetUserAsync(2)
);

// Flow.Any returns the result of the first flow to complete successfully.
var anyFlow = Flow.Any(
    GetUserFromCacheAsync(1),
    GetUserFromDbAsync(1)
);
```

### 3. Enriching Flows with Behaviours

Behaviours enrich a flow with cross-cutting concerns.

#### 3.1 Timing out an Operation

```csharp
// Simple usage
var simpleTimeoutFlow = CreateLongRunningFlow()
    .WithTimeout(TimeSpan.FromSeconds(1));

// Advanced usage with a policy
// (Note: The specific builder methods and policy properties are illustrative and TBD.)
var policy = new FlowTimeoutPolicy(TimeSpan.FromSeconds(1));
var advancedTimeoutFlow = CreateLongRunningFlow()
    .WithTimeout(policy);
```

#### 3.2 Retrying a Failed Operation

```csharp
// Simple usage
var simpleRetryFlow = CreateSometimesFailingFlow()
    .WithRetry(3);

// Advanced usage with a policy
// (Note: The specific builder methods and policy properties are illustrative and TBD.)
var policy = new FlowRetryPolicy(maxAttempts: 3);
var advancedRetryFlow = CreateSometimesFailingFlow()
    .WithRetry(policy);
```

#### 3.3 Applying Custom Behaviours

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