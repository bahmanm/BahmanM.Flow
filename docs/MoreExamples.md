# Practical Recipes

Ready to cook up something more advanced? This document provides practical recipes for specific scenarios you might encounter.

# Handling Concurrency

### Running Multiple Flows at Once

**Problem:** You have several long-running `Flows` and you want to run them all in parallel and collect the results.

**Solution:** Use `Flow.All`. It's like `Task.WhenAll` for Flows. 
* It runs all Flows concurrently and, if they all succeed, returns a Flow containing an array of their results. 
* If any Flow fails, the entire operation fails immediately.

```csharp
var allUsersFlow = Flow.All(
    GetUserAsync(1),
    GetUserAsync(2),
    GetUserAsync(3)
);
```

### Racing Flows for the Fastest Success

**Problem:** You have multiple sources for the same data (e.g., a cache and a database), and you want the result from whichever one finishes first.

**Solution:** Use `Flow.Any`. 
* It's like `Task.WhenAny`, but it specifically waits for the first Flow to succeed. 
* This is perfect for building fast, resilient data retrieval.

```csharp
var fastestUserFlow = Flow.Any(
    GetUserFromCacheAsync(1), // This one is probably faster
    GetUserFromDbAsync(1)
);
```

# Building Resiliency

> **Note:** The resiliency operators are actually built-in _Behaviours_. 
> You can also [create your own custom behaviours](./CustomBehaviours.md) for more complex scenarios like circuit breakers.

### Retrying Failed Operations

**Problem:** An operation in your Flow might fail intermittently due to a flaky network or a temporary service outage.

**Solution:** Use the `.WithRetry()` operator to automatically retry a failed operation.

```csharp
var resilientFlow = CreateSometimesFailingFlow()
    .WithRetry(3); // Tries up to 3 times before giving up
```

### Preventing Hung Operations

**Problem:** An operation in your Flow might hang indefinitely, tying up resources.

**Solution:** Use the `.WithTimeout()` operator to enforce a deadline.

```csharp
var timelyFlow = CreateLongRunningFlow()
    .WithTimeout(TimeSpan.FromSeconds(5)); // Gives up if it takes too long
```

# Managing Resources

### Handling Disposable Resources

**Problem:** You need to use a resource that must be disposed of (like an `HttpClient` or a database connection) within a Flow.

**Solution:** Use `Flow.WithResource`. 
* It ensures your resource is created, used, and then safely disposed of.
* It mirros a standard `using` block but in a **composable** way!

```csharp
var resourceFlow = Flow.Succeed("user-123")
    .Chain(userId =>
        Flow.WithResource(
            acquire: () => new HttpClient(),
            use: httpClient => NotifyServiceAsync(httpClient, userId)
        )
    );
```


# What's Next?

Now that you've seen some advanced use cases, you have a solid overview of what Flow can do.

Depending on your interest, you can now:
*   Look at **[Behaviours](./CustomBehaviours.md)** to learn about the concept and how to roll your own.
*   Read the **[Design Rationale](./DesignRationale.md)** to understand the "why" behind the library.
*   Browse the **[API Blueprint](./ApiBlueprint.cs)** to see all available methods.
