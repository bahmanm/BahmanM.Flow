# Practical Recipes

Ready to cook up something more advanced? This document provides practical recipes for specific scenarios you might encounter.

# Concurrency

### Running Multiple Operations at Once

**Problem:** You have several long-running operations and you want to run them all in parallel and collect the results.

**Solution:** Use `Flow.All`. 
* It's like `Task.WhenAll` for `Flows`. 
* It runs all `Flows` concurrently and, if they all succeed, returns a `Flow` containing an array of their results. 
* If any `Flow` fails, the entire operation fails immediately.

```csharp
var allUsersFlow = Flow.All(
    GetUserAsync(1),
    GetUserAsync(2),
    GetUserAsync(3)
);
```

### Racing Operations for the Fastest Success

**Problem:** You have multiple sources for the same data (e.g., a cache and a database), and you want the result from whichever one finishes first.

**Solution:** Use `Flow.Any`.
* It's like `Task.WhenAny`, but it specifically waits for the first `Flow` to succeed.
* `Flow.Any` will cancel all losing branches as soon as the first success arrives — provided those branches are built from cancellable operations.
* Prefer the cancellable `Flow.Create((ct) => Task<T>)` and cancellable operator overloads to enable prompt co‑operative cancellation.

```csharp
// Prefer cancellable flows so losing branches stop quickly.
var fastestUserFlow = Flow.Any(
    Flow.Create<User>(async ct => await GetUserFromCacheAsync(1, ct)),
    Flow.Create<User>(async ct => await GetUserFromDbAsync(1, ct))
); // losers observe cancellation as soon as the winner succeeds
```

## Resiliency Behaviours

> **Note:** The following behaviours (`.With...`) are powered by `Flow`'s **Behaviour** system. You can learn more in **[Behaviours](./Behaviours.md)**.

### Retrying a Failed Operation

**Problem:** An operation in your `Flow` might fail intermittently due to a flaky network or a temporary service outage.

**Solution:** Use the `.WithRetry()` behaviour to automatically retry a failed operation.

```csharp
var resilientFlow = CreateSometimesFailingFlow()
    .WithRetry(3); // Tries up to 3 times before giving up
```

### Preventing a Hung Operation

**Problem:** An operation in your `Flow` might hang indefinitely, tying up resources.

**Solution:** Use the `.WithTimeout()` behaviour to enforce a deadline.

```csharp
var timelyFlow = CreateLongRunningFlow()
    .WithTimeout(TimeSpan.FromSeconds(5)); // Gives up if it takes too long
```

### Combining Resiliency with Recovery

**Problem:**

You want to build a truly robust Flow.

It should handle transient failures and unexpected hangs.

But it must still provide a fallback value if all else fails.

**Solution:**

Combine `.WithRetry()` and `.WithTimeout()` with `.Recover()`.

This creates a powerful, multi-layered resiliency strategy.

Flow will first attempt the operation.

Then, it will retry on failure.

Finally, it will recover if all retries fail or a timeout occurs.

```csharp
var superResilientFlow = CreateFlakyAndSlowFlow()
    .WithTimeout(TimeSpan.FromSeconds(10)) // 1. Enforce a 10-second deadline.
    .WithRetry(3)                         // 2. Retry up to 3 times on failure.
    .DoOnFailure(ex => _logger.LogError(ex, "The operation ultimately failed."))
    .Recover(ex => GetDefaultValue());    // 3. If all else fails, recover.
```

> [!NOTE]
> 
> **Execution Order Matters:**
>
> The order in which you apply these behaviours is crucial.
>
> In the example above, the timeout wraps the entire retry logic.
>
> This means the 10-second limit applies to the total time for all attempts.
>
> If you applied `.WithRetry()` first, each attempt would get its own timeout.

### Racing With Cancellation (End‑to‑End)

**Problem:** You want to race multiple sources, return the first success, and ensure the others stop immediately to save resources.

**Solution:** Use `Flow.Any` with cancellable operations.

```csharp
var userId = 1;

// Build cancellable flows so the race can cancel losers.
var fromCache = Flow.Create<User>(async ct => await GetUserFromCacheAsync(userId, ct));
var fromDb    = Flow.Create<User>(async ct => await GetUserFromDbAsync(userId, ct));

var firstWins = Flow.Any(fromCache, fromDb)
    .DoOnSuccess(u => _logger.LogInformation($"Winner: {u.Id}"));

var outcome = await FlowEngine.ExecuteAsync(firstWins);
```

# Resource Management

### Working With `IDisposable`s

**Problem:** You need to use a resource that requires safe disposal, like an `HttpClient`, in the middle of a complex pipeline.

**Solution:** Use `Flow.WithResource`. 
*  It perfectly mirrors a `using` block, but in a way that composes beautifully with the rest of your Flow.
* It ensures your resource is created, used, and then safely disposed of, regardless of whether the operation succeeds or fails.

Here's how you would use it to make an API call:

```csharp
var userProfileFlow = Flow.Succeed("user-123")
    .Chain(userId =>
        Flow.WithResource(
            acquire: () => new HttpClient(),
            use: httpClient =>
                // This is a new pipeline that only runs
                // within the scope of the HttpClient.
                Flow.Create(async () => await httpClient.GetAsync($"/users/{userId}"))
                    .Chain(response => ProcessHttpResponseFlow(response))
        )
    );
```

> _**A Note for the Curious:** This pattern is often used inside a `.Chain()` because the operation that needs the resource (like fetching a user profile) usually depends on the output of a previous step (like a `userId`).
> Since the entire "acquire-use-dispose" block is a single, failable unit of work, it fits perfectly within `.Chain()`, which is designed for sequencing failable operations._

## What's Next?

Now that you've seen some practical recipes, you can dive deeper into the concepts that power them.

*   Learn to build your own behaviours in **[Behaviours](./Behaviours.md)**.
*   Read the **[Design Rationale](./DesignRationale.md)** to understand the "why" behind Flow.
*   Browse the **[API Blueprint](./ApiBlueprint.cs)** to see all available methods.
