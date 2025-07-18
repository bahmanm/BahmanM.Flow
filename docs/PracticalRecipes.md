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
* This is perfect for building fast, resilient data retrieval.

```csharp
var fastestUserFlow = Flow.Any(
    GetUserFromCacheAsync(1), // This one is probably faster
    GetUserFromDbAsync(1)
);
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
*   Read the **[Design Rationale](./DesignRationale.md)** to understand the "why" behind the library.
*   Browse the **[API Blueprint](./ApiBlueprint.cs)** to see all available methods.
