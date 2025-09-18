# Your First Flow

Let's look at a real-world example. 

### The 'Before'

Imagine having to write this chunk of imperative code to handle a simple user onboarding step. 

It's defensive, noisy, and hard to follow. The core business logic is completely obscured by `try/catch` blocks, logging, and metrics.

```csharp
async Task<Guid> SendWelcomeAsync(int userId)
{
    User? user;
    try
    {
        user = await _users.GetAsync(userId);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "User lookup threw for {UserId}", userId);
        _metrics.Increment("emails.lookup.exceptions");
        throw;
    }

    if (user is null)
    {
        _logger.LogWarning("User not found: {UserId}", userId);
        _metrics.Increment("emails.lookup.not_found");
        throw new NotFoundException("User");
    }

    // ... more try/catch blocks for templating and sending ...
}
```

### The 'After'

With Flow, you can refactor that into a clean, declarative pipeline that clearly expresses the sequence of operations.

```csharp
var onboardingFlow =
    Flow.Succeed(userId)
        .Chain(Users.FindUserFlow)
        .Validate(
            user => user is not null, 
            _ => new NotFoundException($"{userId}"))
        .Chain(user => 
            user.IsVip switch {
              true =>  Flow.Succeed(Templates.VipWelcomeEmailFor(user)),
              false => Flow.Succeed(Templates.StandardWelcomeEmailFor(user))
            })
        .Chain(Emails.SendWelcomeEmailFlow)
        .DoOnSuccess(_ => 
            _metrics.Increment("emails.sent"))
        .DoOnFailure(ex => 
            _logger.LogError(ex, "Send failed"));

await FlowEngine.ExecuteAsync(onboardingFlow);
```

### The Core Recipe

This pipeline is made possible by a small set of composable building blocks. Here are the core components used above:

```csharp
// --- 1. Starting a Flow ---
var a = Flow.Succeed(42);
var b = Flow.Create(() => GetValueFromDatabase());

// --- 2. Composing a Pipeline ---
var transformed = initialFlow.Select(i => i.ToString());      // T -> U
var sequenced   = initialFlow.Chain(i => GetNextFlow(i));      // T -> IFlow<U>
var validated   = initialFlow.Validate(i => i > 0, _ => new Exception("..."));
var logged      = initialFlow.DoOnSuccess(i => Log(i));

// --- 3. Executing the Flow ---
Outcome<string> outcome = await FlowEngine.ExecuteAsync(sequenced);
```

---

### Next Up

Now that you've seen a basic pipeline, let's look at more advanced composition techniques.

*   **[Advanced Concepts](./advanced-concepts.md)**
