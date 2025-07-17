# Meet the Core Toolkit

Building a Flow is like assembling a team of specialists. Each one has a specific job. Let's meet the main players you'll be working with.

### 1. The Starters: `Flow.Succeed()`, `Flow.Fail()`, `Flow.Create()`

This is your starting point. Every Flow begins here.

*   `Flow.Succeed(value)`: Use this when you already have a value and want to bring it into the `Flow` world.
*   `Flow.Fail(exception)`: Use this to explicitly start a pipeline in a failed state. It's perfect for short-circuiting a workflow after some initial validation fails.
*   `Flow.Create(action)`: Use this when your starting point is a function that might succeed or fail (like a database call or an API request). `Create` wraps that function, capturing its success or failure outcome.

### 2. The Transformer: `.Select()`

`.Select()` is your specialist for transforming the **value** inside a successful Flow.

Think of it like LINQ's `Select`. It takes a function that maps the input value to a new output value (e.g., `Func<TIn, TOut>`). If that function throws an exception during the transformation (for example, a deserialization error), the `Flow` will **automatically catch it** and transition to a `Failure` state.

```csharp
// This function might throw if the request is malformed.
var command = CreateCommandFromRequest(request);

// .Select() will safely handle the potential exception from the transformation.
var commandFlow = Flow.Succeed(request)
    .Select(req => CreateCommandFromRequest(req));
```

### 3. The Sequencer: `.Chain()`

`.Chain()` is your specialist for connecting to the **next Flow**.

You use `.Chain()` when the next step in your process is an action that returns another `Flow`. 

This is the key to building complex pipelines: It takes the result of one step and "chains" it to the next operation, keeping the pipeline **clean and flat** and avoiding nested `Flow<Flow<T>>`.

```csharp
// GetUserFromApiFlow itself returns an IFlow<User>.
// .Chain() ensures the result is a simple IFlow<User>.
var userFlow = userIdFlow.Chain(id => GetUserFromApiFlow(id));
```

### 4. The Safety Net: `.Recover()`

No matter how well you plan, things go wrong. `.Recover()` is your contingency plan.

It attaches to a Flow and specifies what to do if any preceding step fails. 

You can use it to provide a default value, run a backup operation, or gracefully handle an exception.

```csharp
// If the API call fails, return a default user instead of blowing up.
var safeUserFlow = userFlow.Recover(exception => GetDefaultUser());
```

### 5. The Bystander: `.DoOn...()`

Sometimes you just need to *do* something without changing the result. 

This is the job of the `.DoOnSuccess()` and `.DoOnFailure()` family.

Like a bystander on the sidelines, they let you peek inside the `Flow` as it passes by, perform a side-effect (like logging), and then let the original outcome continue on its way, untouched.

```csharp
// Log the user's name on success, or the error on failure.
var observedFlow = userFlow
    .DoOnSuccess(user => _logger.Log($"Got user: {user.Name}"))
    .DoOnFailure(ex => _logger.LogError(ex, "Failed to get user"));
```
