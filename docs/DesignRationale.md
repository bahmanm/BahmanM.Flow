# Design Rationale

This section documents the key architectural decisions that shape the Flow API, focusing on the "why" behind the design.

# 1. The Core Philosophy: Recipe & Chef

The central architectural pattern is a strict separation of concerns:

*   **`IFlow<T>` is the Recipe:** It is a purely declarative, **immutable** data structure representing the sequence of operations. It is an Abstract Syntax Tree (AST) that defines *what* to do, not *how* to do it.

*   **`FlowEngine` is the Chef:** It is the interpreter that knows how to execute an `IFlow<T>` AST. It is the single source of execution logic.

*   **Operators are Pure Functions on the Flow:** With the exception of the `Do` family of operators, all extension methods in `FlowExtensions` (e.g., `.Chain()`, `.Select()`, `.Recover()`) are themselves pure functions with respect to the `IFlow<T>` data structure. They take a flow as input and return a **new, decorated `IFlow<T>` instance**, never modifying the original. This guarantees that flow declarations are immutable, reusable, and safe to share.

*   **The `Do` Operators are Transparent:** The `DoOnSuccess` and `DoOnFailure` operators are an exception to the "return new instance" rule. They are designed for pure side effects and are intended to be completely transparent to the flow's structure.

*   **Execution Starts from the End:** When `FlowEngine.ExecuteAsync` is called on an `IFlow<T>` instance, it executes the *entire chain* of operations, not just the final one. The engine first walks backwards from the provided flow through its `Upstream` properties to find the original source, and then executes the entire sequence. This ensures that the result of a flow is always complete and predictable, regardless of which link in the chain you hold a reference to.

This separation ensures the business logic declared in a `Flow` remains pure and testable, while the complexities of execution are handled by the engine.

### 1a. The Philosophy: Pragmatism over Purity

While Flow is heavily inspired by functional programming concepts, it is not a strict FP framework. 

The primary goal is to provide an intuitive, discoverable, and productive API for the typical C# developer.

This means the library will always favour a design that is **pragmatic and familiar** over one that is theoretically pure but abstract. 

A good example of this is the approach to concurrency:

*   Flow provides `Flow.All` and `Flow.Any`, which are direct conceptual mirrors of `Task.WhenAll` and `Task.WhenAny`. A C# developer will instantly understand what these do.

*   A purist approach might have offered a single, more powerful `Traverse` function. However, that would require users to understand concepts like Applicative Functors, creating a steep learning curve.

The design choice is to provide specific, named solutions to common problems rather than a single, generic tool that requires academic knowledge to use. 

This principle of pragmatism guides the entire API design.

(For those interested, a more detailed mapping of Flow concepts to their functional counterparts can be found in [Notes for FP Developers](./ForFPDevelopers.md).)

# 2. `FlowExecutionOptions`: Decoupling Execution from Declaration

All parameters that control a specific execution run (the "how-to-run" instructions) are contained within the `FlowExecutionOptions` object. 

This includes the `CancellationToken` and the `IFlowExecutionObserver`. 

This approach keeps the `FlowEngine.ExecuteAsync` signature stable and makes the API extensible, as new execution-time options can be added without breaking changes.

# 3. The Observer Pattern for Diagnostics

To provide visibility into the engine's execution, Flow is designed with a decoupled observer pattern rather than baking diagnostics directly in.

*   **`IFlowExecutionObserver`:** This interface defines a contract for observing the engine's lifecycle. 

_It is designed to be implemented by users to bridge `Flow` events to their chosen diagnostic or logging framework._

*   **Multi-Method Interface:** The interface uses specific, discoverable methods (`OnOperationStarted`, `OnFlowSucceeded`, etc.) with default implementations. 

_This provides the best of both worlds: it's easy for users to implement (they only override what they need), and it's extensible for the library (new methods can be added without breaking existing implementations)._

*   **Interface-Based Events:** The events passed to the observer are defined by a hierarchy of interfaces (`IFlowEvent`, `IOperationEvent`, etc.). 

_This allows for flexible, type-safe pattern matching in observer implementations while decoupling the observer from the concrete event implementation classes._

# 4. Extensible Enumerations: The `FlowOperationTypes` Pattern

To identify the type of operation in a diagnostic event, Flow uses a `string` property (`IOperationEvent.OperationType`).

To provide compile-time safety for built-in operations, Flow additionally provides a `public static class FlowOperationTypes` containing `const string` definitions. 

This is a standard .NET library pattern that provides the safety of an `enum` for known types and the extensibility of a `string` for user-defined custom operations.

# 5. Strongly-Typed IDs

Instead of using primitive `string` types for flow and operation identifiers, Flow uses dedicated `FlowId` and `OperationId` classes. 

This is a best practice that:
*   Improves type safety throughout the API.
*   Encapsulates the ID generation logic (`FlowId.Generate()`).
*   Makes the code more self-documenting.

# 6. The `Failure` Model

A `Failure<T>` in the `Outcome<T>` model always contains an `Exception`. 

This was a deliberate design choice to align with standard .NET idioms. 

"Business-level" failures (e.g., validation errors) should be modeled as a type of `Success<T>` (e.g., `Success<ValidationResult>`), while the `Failure` path is reserved for true, exceptional circumstances that disrupt the normal flow of a computation.

# 7. Resource Management Patterns

The Flow library provides a single, clear pattern for managing resources.

*   **`Flow.WithResource`:** This factory is used for **isolated, operation-scoped resources**. The mandatory `acquire` function makes it explicit that a new resource is created for the scope of the `use` function. The `where TResource : IDisposable` constraint ensures that the resource is properly disposed of by the engine, mirroring the behavior of a standard C# `using` block. It is the correct tool for encapsulated resources, like an `HttpClient` for a single API call.

*   **Shared Resources (Deferred):** The problem of managing resources that must be shared across multiple, independent operations (e.g., a `DbTransaction`) is a more advanced use case. The exploration of a `FlowEnvironment` (`Reader` monad) pattern to solve this has been deferred to a future work item (`dx006`) to keep the core API lean and focused.
