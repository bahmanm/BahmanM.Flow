# The Core Operators

An **Operator** is a foundational primitive that you use to build a pipeline, step-by-step, operation-by-operation.

They are the verbs of the Flow language, each responsible for a single, specific part of the pipeline.

> **Heads Up!** All operators have built-in support for `async/await`. For simplicity, the examples below use synchronous code, but each one has an `async` overload waiting for you when you need it.

### `Flow.Create()`, `Flow.Succeed()`, `Flow.Fail()`

These are the **Starters**. 

Every Flow begins with one of these.

*   `Flow.Succeed(value)`: Use this when you already have a value and want to bring it into the Flow world.
*   `Flow.Fail(exception)`: Use this to explicitly start a pipeline in a failed state.
*   `Flow.Create(operation)`: Use this when your starting point is an **operation** (a function) that might succeed or fail, like a database call.

### `.Select()`

This is the **Transformer**. 

It is your specialist for transforming the value inside a successful Flow.

Think of it like LINQ's `Select`. It takes a transformation operation (`TIn -> TOut`). If the operation throws an exception, Flow will automatically catch it and transition the pipeline to a `Failure` state.

```csharp
// From a Flow containing a Request to a Flow containing a Command
var commandFlow = Flow.Succeed(request)
    .Select(req => CreateCommandFrom(req)); // This operation can throw
```

### `.Chain()`

This is the **Sequencer**.

It is the primary link for connecting one Flow to the next.

You use `.Chain()` when your next operation returns another Flow. It takes the result of one step and sequences it to the next, keeping the pipeline clean and flat.

```csharp
// The GetUserFromApiFlow operation itself returns an IFlow<User>.
var userFlow = userIdFlow.Chain(id => GetUserFromApiFlow(id));
```

### `.Validate()`

This is the **Gatekeeper**.

It is your steadfast ally in enforcing business rules and invariants on the value inside a successful Flow.

You use `.Validate()` to check if the data is in a valid state to continue. It takes a predicate that returns `true` if the data is valid. If the predicate returns `false`, the Gatekeeper stops the "happy path" and transitions the pipeline to a `Failure` state, using the exception you provide.

```csharp
// Ensure the user is an administrator before allowing them to delete a resource.
var validatedAdminFlow = userFlow
    .Validate(
        user => user.IsAdmin,
        user => new AuthorizationException($"User '{user.Name}' is not an administrator.")
    );
```

### `.Recover()`

This is the **Safety Net**.

It's your contingency plan for when things go wrong.

It ensures a Flow can continue even after a failure.

*   **Use `.Recover(fallbackValue)` when** you have a simple, static default.
*   **Use `.Recover(recoveryFunc)` when** you need to compute a new value from the exception details.
*   **Use `.Recover(recoveryFlowFunc)` when** your recovery logic is itself a failable operation, like trying a cache.

```csharp
// If fetching the user fails, create a temporary guest user to continue the flow.
var safeUserFlow = userFlow
    .DoOnFailure(ex => _logger.LogWarning(ex, "Could not fetch primary user."))
    .Recover(ex => new User(isGuest: true, error: ex.Message));
```

> **Connecting the Gatekeeper and the Safety Net**
>
> While `.Validate()` is powerful for stopping a flow, its true potential is unlocked when paired with `.Recover()`. This combination allows you to create sophisticated conditional logic. You can `Validate` a condition and, if it fails, `Recover` into an alternative, successful workflow.
>
> This creates a declarative `if/else` for your entire pipeline.
>
> ```csharp
> var processedFlow = dataFlow
>     // If the data is "special", continue the main flow...
>     .Validate(
>         data => data.IsSpecial,
>         data => new NormalDataException($"Data {data.Id} is not special.")
>     )
>     .Chain(specialData => ProcessSpecialDataFlow(specialData))
>     // ...otherwise, recover from the validation failure and process it normally.
>     .Recover(ex => ex is NormalDataException,
>         () => ProcessNormalDataFlow(originalData)
>     );
> ```

### `.DoOn...()`

This is the **Bystander**.

It lets you perform a side-effect without changing the result.

Like a bystander on the sidelines, it lets you peek inside the Flow as it passes by to perform an action (like logging), and then lets the original outcome continue on its way, untouched.

```csharp
var observedFlow = userFlow
    .DoOnSuccess(user => _logger.Log($"Got user: {user.Name}"))
    .DoOnFailure(ex => _logger.LogError(ex, "Failed to get user"));
```

---

### Rule of Thumb: `Select` vs. `Chain`

This is an important distinction to remember:

*   **Use `.Select()` when** your operation transforms a value and returns a **plain object** (`TIn -> TOut`).
*   **Use `.Chain()` when** your operation kicks off a new process and returns **another Flow** (`TIn -> IFlow<TOut>`).

---

## What's Next?

You've met the core operators! With these, you can build almost any Flow.

When you're ready, you can see how to combine these in **[Practical Recipes](./PracticalRecipes.md)**.
