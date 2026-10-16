# λ For Functional Programmers

If you have a background in functional programming, you may recognize some familiar patterns in Flow. 

This section provides a brief mapping from Flow concepts to their functional programming counterparts.

*   **`IFlow<T>` as a Monad:** Flow can be understood as a monadic abstraction for composing asynchronous, failable operations. A flow carries a value through operations while preserving the possibility of failure.

*   **`Outcome<T>` as `Either<Exception, T>`:** The result of executing a flow is either `Success<T>` or `Failure<T>`, analogous to `Right<T>` and `Left<Exception>` in `Either`.

*   **`Flow.Succeed(value)` as `return` or `pure`:** This places an existing value into a successful flow.

*   **`Flow.Create()` as `liftF` or `IO.delay`**: This function captures an effectful computation (e.g., an I/O operation or a function that might throw an exception) and defers its execution until the flow is run. It's analogous to `liftF` in a Free monad context or `IO.delay`/`IO.suspend` in libraries like Cats Effect. Exceptions thrown by the operation become flow failures.

*   **`.Chain()` as `bind` or `flatMap`:** It takes a value from a successful flow, applies a function that returns another flow (`A -> M<B>`), and sequences that child flow.

*   **`.Select()` as `map` or `fmap`:** It applies a function (`A -> B`) to the value in a successful flow. If the callback throws, Flow converts that exception into a `Failure`.

*   **`FlowEngine` as the Interpreter:** The public API composes a description, which `FlowEngine` lowers to an AST and interprets to produce an `Outcome<T>`. Flows returned by callbacks such as those passed to `Chain` are produced during execution and lowered then.

---

# A Note on Pragmatism and C#

While Flows's core ideas are built around the functional foundations of effect systems, its public API is intentionally designed to be pragmatic and familiar to a developer accustomed to standard .NET patterns.

This design philosophy is a direct consequence of the C# language itself, specifically its lack of higher-kinded types (HKTs) and universal abstractions like type classes (e.g., `IMonad<T>`).

Without those features, it's impossible to create a single, generic `Traverse` function or a truly universal `Select` that works across all "monadic" types.

As a result, Flow makes a deliberate trade-off:

*   It provides concrete, named methods like `Flow.All` and `Flow.Any` that mirror the well-known `Task.WhenAll` and `Task.WhenAny`, rather than exposing a single, more abstract `Traverse` function with different `Applicative` implementations for you to choose from.
*   It focuses on providing a single, powerful Flow type that solves a specific set of problems, rather than attempting to be a generic functional toolkit.

The goal is to leverage the power and safety of functional patterns internally and where it helps Flows be more robust, while presenting a simple, intuitive, and productive toolkit externally. 

The rich functional nature of Flow is an implementation detail, not a user-facing prerequisite.
