# λ For Functional Programmers

If you have a background in functional programming, you may recognize some familiar patterns in Flow. 

This section provides a brief mapping from Flows concepts to their traditional FP counterparts.

*   **`IFlow<T>` as a Monad:** At its core, Flows is a monad for managing asynchronous, failable operations. It encapsulates a value and a context (in this case, the potential for failure and the sequence of operations).

*   **`Outcome<T>` as `Either<Exception, T>`:** The `Outcome<T>` type, which is the result of executing a Flows, is a classic sum type representing one of two possible outcomes. It is directly analogous to the `Either` monad, where `Success<T>` corresponds to `Right<T>` and `Failure<T>` corresponds to `Left<Exception>`.

*   **`Flow.Succeed()` and `Flow.Of()` as `return` or `pure`:** These are the monadic unit functions. They take a simple value and lift it into the monadic context (Flows). `Flow.Succeed()` is an alias for `Flow.Of()`.

*   **`Flow.Create()` as `liftF` or `IO.delay`**: This function captures an effectful computation (e.g., an I/O operation or a function that might throw an exception) and defers its execution. It's analogous to `liftF` in a Free monad context or `IO.delay`/`IO.suspend` in libraries like Cats Effect. It allows you to bring impure actions into the `Flow` context safely.

*   **`.Chain()` as `bind` or `flatMap`:** This is the quintessential monadic binding function (`>>=`). It takes a value from a monadic context, applies a function that returns a new monad (`A -> M<B>`), and flattens the result (`M<B>`). This is the foundation of sequencing in Flows.

*   **`.Select()` as `map` or `fmap`:** This is the functorial `map`. It allows you to apply a pure function (`A -> B`) to the value inside the monadic context without affecting the context itself. Flows's implementation also includes exception handling, automatically lifting a thrown exception into a `Failure` state.

*   **`FlowEngine` as the Interpreter:** The `FlowEngine` acts as the interpreter that "runs" the monadic computation. It traverses the constructed Flows (which is essentially an Abstract Syntax Tree) and executes the described effects, ultimately producing an `Outcome<T>`.

---

# A Note on Pragmatism and C#

While Flows's core ideas are built around the functional foundations of effect systems, its public API is intentionally designed to be pragmatic and familiar to a developer accustomed to standard .NET patterns.

This design philosophy is a direct consequence of the C# language itself, specifically its lack of higher-kinded types (HKTs) and universal abstractions like type classes (e.g., `IMonad<T>`).

Without those features, it's impossible to create a single, generic `Traverse` function or a truly universal `Select` that works across all "monadic" types.

As a result, the library makes a deliberate trade-off:

*   It provides concrete, named methods like `Flow.All` and `Flow.Any` that mirror the well-known `Task.WhenAll` and `Task.WhenAny`, rather than exposing a single, more abstract `Traverse` function with different `Applicative` implementations for you to choose from.
*   It focuses on providing a single, powerful Flow type that solves a specific set of problems, rather than attempting to be a generic functional toolkit.

The goal is to leverage the power and safety of functional patterns internally and where it helps Flows be more robust, while presenting a simple, intuitive, and productive toolkit externally. 

The rich functional nature of the library is an implementation detail, not a user-facing prerequisite.
