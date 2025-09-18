# Get Started With Flow Right Now

Is your business logic a tangled mess of `try-catch` blocks and `if/else` statements? Do you find error handling, logging, and retries cluttering your core code?

What if you could refactor that complexity into a clean, chainable sequence of operations instead? A sequence that is a pleasure to express, read, and maintain?

This is the problem that **Flow** is designed to solve.

### The Core Idea: Recipe & Chef

Flow is built on a simple analogy:

*   A **Flow** is a **recipe**—an immutable, declarative blueprint that describes the steps in your process.
*   The **`FlowEngine`** is the **chef**—it takes your recipe and executes it, handling the messy details of `async` operations and exceptions.

This separation makes your logic easier to read, test, and reuse.

### An Example

Imagine turning this...

```csharp
public async Task<string> GetUserName(int id)
{
    try
    {
        var user = await _database.GetUserAsync(id);
        if (user != null)
        {
            return user.Name;
        }
        throw new Exception("User not found");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to get user");
        throw;
    }
}
```

...into this:

```csharp
public IFlow<string> GetUserNameFlow(int id) =>
    Flow.Create(() => _database.GetUserAsync(id))
        .Validate(user => user is not null, _ => new Exception("User not found"))
        .Select(user => user.Name)
        .DoOnFailure(ex => _logger.LogError(ex, "Failed to get user"));
```

---
## Where to Go Next?

You've seen the basic idea. To learn about the building blocks used in the example above, like `.Validate`, `.Select`, and `.DoOnFailure`, your next stop is the guide to the Core Operators.

*   **[Deep Dive: The Core Operators](./core-operators.md)**
