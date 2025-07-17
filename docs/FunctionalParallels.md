## λ For Functional Programmers

If you have a background in functional programming, you may recognize some familiar patterns in `Flow`. This section provides a brief mapping from `Flow` concepts to their traditional FP counterparts.

*   **`IFlow<T>` as a Monad:** At its core, `Flow` is a monad for managing asynchronous, failable operations. It encapsulates a value and a context (in this case, the potential for failure and the sequence of operations).

*   **`Outcome<T>` as `Either<Exception, T>`:** The `Outcome<T>` type, which is the result of executing a `Flow`, is a classic sum type representing one of two possible outcomes. It is directly analogous to the `Either` monad, where `Success<T>` corresponds to `Right<T>` and `Failure<T>` corresponds to `Left<Exception>`.

*   **`Flow.Succeed()` as `return` or `pure`:** This is the monadic unit function. It takes a simple value and lifts it into the monadic context (`Flow`).

*   **`.Chain()` as `bind` or `flatMap`:** This is the quintessential monadic binding function (`>>=`). It takes a value from a monadic context, applies a function that returns a new monad (`A -> M<B>`), and flattens the result (`M<B>`). This is the foundation of sequencing in `Flow`.

*   **`.Select()` as `map` or `fmap`:** This is the functorial `map`. It allows you to apply a pure function (`A -> B`) to the value inside the monadic context without affecting the context itself. Flow's implementation also includes exception handling, automatically lifting a thrown exception into a `Failure` state.

*   **`FlowEngine` as the Interpreter:** The `FlowEngine` acts as the interpreter that "runs" the monadic computation. It traverses the constructed `Flow` (which is essentially an Abstract Syntax Tree) and executes the described effects, ultimately producing an `Outcome<T>`.

This separation of declaration (`IFlow`) from execution (`FlowEngine`) is a deliberate choice to make the monadic nature of the library an implementation detail rather than a prerequisite for users.