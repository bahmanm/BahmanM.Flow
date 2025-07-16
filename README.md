# Flow: A Declarative Framework for Composable Operations

Flow is a lightweight, powerful framework for building robust, predictable, and testable business logic in .NET. It provides a simple, fluent API for composing complex operations, handling failures, and managing cross-cutting concerns in a declarative and immutable way.

# Core Principles

1.  **Immutable Declarations:** All `IFlow<T>` instances are **immutable**. All operators (e.g., `.Chain()`, `.Select()`) do not modify the flow they are called on; they return a **new, decorated `IFlow<T>` instance**. This guarantees that the *declaration* of a flow is reusable and safe to share. The thread safety of the overall *execution* depends on the thread safety of the user-provided delegates.

2.  **Declarative & Lazy:** A `Flow` is a declarative "recipe" for a computation. It defines *what* should happen, not *how* or *when* it should be executed. The entire chain is evaluated lazily by the `FlowEngine`.

3.  **Separation of Concerns:** The declaration of a flow (`IFlow<T>`) is strictly separated from its execution (`FlowEngine`). This allows for a clean, testable, and extensible architecture.

# API Reference


```csharp
//========================================================================================
// BahmanM.Flow
//========================================================================================
namespace BahmanM.Flow
{
    // --- Core Interfaces & Types ---
    public interface IFlow<T> { }

    public abstract record Outcome<T>;
    public sealed record Success<T>(T Value) : Outcome<T>;
    public sealed record Failure<T>(Exception Exception) : Outcome<T>;

    // --- Core Factory & Operators ---
    public static class Flow
    {
        // Creation
        public static IFlow<T> Succeed<T>(T value, FlowId flowId = null);
        public static IFlow<T> Fail<T>(Exception error, FlowId flowId = null);
        public static IFlow<T> Create<T>(Func<Outcome<T>> func, FlowId flowId = null);
        public static IFlow<T> Create<T>(Func<Task<Outcome<T>>> asyncFunc, FlowId flowId = null);

        // Concurrency
        public static IFlow<T[]> All<T>(params IFlow<T>[] flows);
        public static IFlow<T> Any<T>(params IFlow<T>[] flows);

        // Resources
        public static IFlow<T> WithResource<TResource, T>(
            Func<TResource> acquire,
            Func<TResource, IFlow<T>> use,
            OperationId operationId = null) where TResource : IDisposable;
    }

    public static class FlowExtensions
    {
        // Core Operators
        public static IFlow<TOut> Select<TIn, TOut>(this IFlow<TIn> flow, Func<TIn, TOut> selector, OperationId operationId = null);
        public static IFlow<TOut> Select<TIn, TOut>(this IFlow<TIn> flow, Func<TIn, Task<TOut>> asyncSelector, OperationId operationId = null);

        public static IFlow<TOut> Chain<TIn, TOut>(this IFlow<TIn> flow, Func<TIn, IFlow<TOut>> func, OperationId operationId = null);
        public static IFlow<TOut> Chain<TIn, TOut>(this IFlow<TIn> flow, Func<TIn, Task<IFlow<TOut>>> asyncFunc, OperationId operationId = null);

        public static IFlow<T> Recover<T>(this IFlow<T> flow, T fallbackValue, OperationId operationId = null);
        public static IFlow<T> Recover<T>(this IFlow<T> flow, Func<Exception, T> recoveryFunc, OperationId operationId = null);
        public static IFlow<T> Recover<T>(this IFlow<T> flow, Func<Exception, IFlow<T>> recoveryFlowFunc, OperationId operationId = null);
        public static IFlow<T> Recover<T>(this IFlow<T> flow, Func<Exception, Task<T>> asyncRecoveryFunc, OperationId operationId = null);
        public static IFlow<T> Recover<T>(this IFlow<T> flow, Func<Exception, Task<IFlow<T>>> asyncRecoveryFlowFunc, OperationId operationId = null);

        // Side-effect Operators
        public static IFlow<T> DoOnSuccess<T>(this IFlow<T> flow, Action<T> action, OperationId operationId = null);
        public static IFlow<T> DoOnSuccess<T>(this IFlow<T> flow, Func<T, Task> asyncAction, OperationId operationId = null);
        public static IFlow<T> DoOnFailure<T>(this IFlow<T> flow, Action<Exception> action, OperationId operationId = null);
        public static IFlow<T> DoOnFailure<T>(this IFlow<T> flow, Func<Exception, Task> asyncAction, OperationId operationId = null);
    }
}

//========================================================================================
// BahmanM.Flow.Annotations
//========================================================================================
namespace BahmanM.Flow.Annotations
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class FlowExperimentalAttribute : Attribute
    {
        public string Message { get; }
        public FlowExperimentalAttribute(string message);
    }
}

//========================================================================================
// BahmanM.Flow.Behaviours
//========================================================================================
namespace BahmanM.Flow.Behaviours
{
    public interface IBehaviour<T>
    {
        string OperationType { get; }
        IFlow<T> Apply(IFlow<T> originalFlow);
    }

    public interface IFlowPolicy { }

    public static class FlowExtensions
    {
        public static IFlow<T> WithBehaviour<T>(this IFlow<T> flow, IBehaviour<T> behaviour, OperationId operationId = null);
    }
}

//========================================================================================
// BahmanM.Flow.Behaviours.Timeout
//========================================================================================
namespace BahmanM.Flow.Behaviours.Timeout
{
    using BahmanM.Flow.Annotations;

    public sealed class FlowTimeoutPolicy : IFlowPolicy
    {
        public FlowTimeoutPolicy(TimeSpan duration);
    }

    public static class FlowExtensions
    {
        [FlowExperimental("The WithTimeout operator is subject to change.")]
        public static IFlow<T> WithTimeout<T>(this IFlow<T> flow, TimeSpan duration, OperationId operationId = null);
        [FlowExperimental("The policy-based WithTimeout operator is subject to change.")]
        public static IFlow<T> WithTimeout<T>(this IFlow<T> flow, FlowTimeoutPolicy policy, OperationId operationId = null);
    }
}

//========================================================================================
// BahmanM.Flow.Behaviours.Retry
//========================================================================================
namespace BahmanM.Flow.Behaviours.Retry
{
    using BahmanM.Flow.Annotations;

    public sealed class FlowRetryPolicy : IFlowPolicy
    {
        public FlowRetryPolicy(int maxAttempts);
    }

    public static class FlowExtensions
    {
        [FlowExperimental("The WithRetry operator is subject to change.")]
        public static IFlow<T> WithRetry<T>(this IFlow<T> flow, int maxAttempts, OperationId operationId = null);
        [FlowExperimental("The policy-based WithRetry operator is subject to change.")]
        public static IFlow<T> WithRetry<T>(this IFlow<T> flow, FlowRetryPolicy policy, OperationId operationId = null);
    }
}

//========================================================================================
// BahmanM.Flow.Execution
//========================================================================================
namespace BahmanM.Flow.Execution
{
    public static class FlowEngine
    {
        public static Task<Outcome<T>> ExecuteAsync<T>(IFlow<T> flow, FlowExecutionOptions options = null);
    }

    public sealed class FlowExecutionOptions
    {
        public CancellationToken CancellationToken { get; init; }
        public IReadOnlyList<IFlowExecutionObserver> Observers { get; init; }
        public static FlowExecutionOptions Default { get; }
    }
}

//========================================================================================
// BahmanM.Flow.Diagnostics
//========================================================================================
namespace BahmanM.Flow.Diagnostics
{
    // --- Observer ---
    public interface IFlowExecutionObserver
    {
        void OnEvent<T>(T e) where T : IFlowEvent;
        void OnFlowStarted(IFlowStartedEvent e) => OnEvent(e);
        void OnOperationStarted(IOperationStartedEvent e) => OnEvent(e);
        void OnOperationSucceeded(IOperationSucceededEvent e) => OnEvent(e);
        void OnOperationFailed(IOperationFailedEvent e) => OnEvent(e);
        void OnFlowSucceeded(IFlowSucceededEvent e) => OnEvent(e);
        void OnFlowFailed(IFlowFailedEvent e) => OnEvent(e);
    }

    // --- Events ---
    public interface IFlowEvent
    {
        FlowId FlowId { get; }
        DateTime Timestamp { get; }
    }

    public interface IFlowStartedEvent : IFlowEvent { }
    public interface IOperationEvent : IFlowEvent
    {
        string OperationType { get; }
        OperationId OperationId { get; }
    }
    public interface IOperationStartedEvent : IOperationEvent { }
    public interface IOperationSucceededEvent : IOperationEvent { }
    public interface IOperationFailedEvent : IOperationEvent
    {
        Exception Exception { get; }
    }
    public interface IFlowCompletedEvent : IFlowEvent { }
    public interface IFlowSucceededEvent : IFlowCompletedEvent { }
    public interface IFlowFailedEvent : IFlowCompletedEvent
    {
        Exception Exception { get; }
    }
}
```

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

# Design Rationale

This section documents the key architectural decisions that shape the Flow API, focusing on the "why" behind the design.

### 1. The Core Philosophy: Recipe & Chef

The central architectural pattern is a strict separation of concerns:

*   **`IFlow<T>` is the Recipe:** It is a purely declarative, **immutable** data structure representing the sequence of operations. It is an Abstract Syntax Tree (AST) that defines *what* to do, not *how* to do it.
*   **`FlowEngine` is the Chef:** It is the interpreter that knows how to execute an `IFlow<T>` AST. It is the single source of execution logic.
*   **Operators are Pure Functions on the Flow:** With the exception of the `Do` family of operators, all extension methods in `FlowExtensions` (e.g., `.Chain()`, `.Select()`, `.Recover()`) are themselves pure functions with respect to the `IFlow<T>` data structure. They take a flow as input and return a **new, decorated `IFlow<T>` instance**, never modifying the original. This guarantees that flow declarations are immutable, reusable, and safe to share.
*   **The `Do` Operators are Transparent:** The `DoOnSuccess` and `DoOnFailure` operators are an exception to the "return new instance" rule. They are designed for pure side effects and are intended to be completely transparent to the flow's structure. Therefore, they should return the **same `IFlow<T>` instance** they were called on, allowing them to be chained without adding a new node to the AST.

This separation ensures the business logic declared in a `Flow` remains pure and testable, while the complexities of execution are handled by the engine.

### 2. `FlowExecutionOptions`: Decoupling Execution from Declaration

All parameters that control a specific execution run (the "how-to-run" instructions) are contained within the `FlowExecutionOptions` object. This includes the `CancellationToken` and the `IFlowExecutionObserver`. This approach keeps the `FlowEngine.ExecuteAsync` signature stable and makes the API extensible, as new execution-time options can be added without breaking changes.

### 3. The Observer Pattern for Diagnostics

To provide visibility into the engine's execution, we chose a decoupled observer pattern over baking diagnostics directly into the engine.

*   **`IFlowExecutionObserver`:** This interface defines a contract for observing the engine's lifecycle. It is designed to be implemented by users to bridge `Flow` events to their chosen diagnostic or logging framework.
*   **Multi-Method Interface:** The interface uses specific, discoverable methods (`OnOperationStarted`, `OnFlowSucceeded`, etc.) with default implementations. This provides the best of both worlds: it's easy for users to implement (they only override what they need), and it's extensible for the library (new methods can be added without breaking existing implementations).
*   **Interface-Based Events:** The events passed to the observer are defined by a hierarchy of interfaces (`IFlowEvent`, `IOperationEvent`, etc.). This allows for flexible, type-safe pattern matching in observer implementations while decoupling the observer from the concrete event implementation classes.

### 4. Extensible Enumerations: The `FlowOperationTypes` Pattern

To identify the type of operation in a diagnostic event, we use a `string` property (`IOperationEvent.OperationType`). To provide compile-time safety for built-in operations, we also provide a `public static class FlowOperationTypes` containing `const string` definitions. This is a standard .NET library pattern that provides the safety of an `enum` for known types and the extensibility of a `string` for user-defined custom operations.

### 5. Strongly-Typed IDs

Instead of using primitive `string` types for flow and operation identifiers, we use dedicated `FlowId` and `OperationId` classes. This is a best practice that:
*   Improves type safety throughout the API.
*   Encapsulates the ID generation logic (`FlowId.Generate()`).
*   Makes the code more self-documenting.

### 6. The `Failure` Model

A `Failure<T>` in the `Outcome<T>` model always contains an `Exception`. This was a deliberate design choice to align with standard .NET idioms. "Business-level" failures (e.g., validation errors) should be modeled as a type of `Success<T>` (e.g., `Success<ValidationResult>`), while the `Failure` path is reserved for true, exceptional circumstances that disrupt the normal flow of a computation.

### 7. Resource Management Patterns

The Flow library provides a single, clear pattern for managing resources.

*   **`Flow.WithResource`:** This factory is used for **isolated, operation-scoped resources**. The mandatory `acquire` function makes it explicit that a new resource is created for the scope of the `use` function. The `where TResource : IDisposable` constraint ensures that the resource is properly disposed of by the engine, mirroring the behavior of a standard C# `using` block. It is the correct tool for encapsulated resources, like an `HttpClient` for a single API call.

*   **Shared Resources (Deferred):** The problem of managing resources that must be shared across multiple, independent operations (e.g., a `DbTransaction`) is a more advanced use case. The exploration of a `FlowEnvironment` (`Reader` monad) pattern to solve this has been deferred to a future work item (`dx006`) to keep the core API lean and focused.
