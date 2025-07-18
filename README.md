# Flow: Clean, Composable Business Logic for .NET

* ❌ Is your business logic a tangled, and potentially ugly, mess?
* ❌ Are there `try-catch` blocks and `if-else` statements everywhere?
* ❌ Do you see side-effects, error handling, logging, retries, and more all over the place?

_Ugh_ 😣

---

* ✅ WHAT IF you could build your workflow as a clean, chainable pipeline of operations instead? 
* ✅ A pipeline that clearly separates the "happy path" from error handling, logging, retries, ...
* ✅ A pipeline that is a pleasure to express, read, and maintain?

_Oh!?_ 🤔

--- 

THAT, my dear reader, is the problem **Flow** solves 🙌

* Lightweight
* Fluent API
* To build pipelines that are:
  * Declarative
  * Resilient
  * Composable
  * Easy to test

---

Allow me to demonstrate. Imagine turning this imperative code:

```csharp
public User GetUserAndNotify(int userId)
{
    try
    {
        var user = _database.GetUser(userId);
        _auditor.LogSuccess(user.Id);
        return user;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to get user");
        return GetDefaultUser();
    }
}
```

Into this simple Flow:

```csharp
public Flow<User> GetUserAndNotifyFlow(int userId)
{
    return Flow.Create(() => _database.GetUser(userId))
               .DoOnSuccess(user => _auditor.LogSuccess(user.Id))
               .DoOnFailure(ex => _logger.LogError(ex, "Failed to get user"))
               .Recover(ex => GetDefaultUser());
}
```

_Nice and neat, eh!?_ 👍

---

But...the REAL win is in Flow's **plug-and-play design** 🔌
* A Flow is just a **recipe** for your business logic.
* Since it is nothing more than a definition, it can be enriched and reused: cheap and simple.
* You can enhance any Flow with new behaviours without ever touching the original code -- no, seriously 😎

---

Allow me to demonstrate:

1. Say, next sprint, you realise you need retry logic? Easy -- you simply enrich your existing flow!

```csharp
var resilientGetUserFlow = 
    GetUserAndNotifyFlow(httpRequestParams.userId)
      .WithRetry(3);
```

2. Or maybe you want to add a timeout? No problem!

```csharp
var timeoutGetUserFlow = 
    resilientGetUserFlow 
      .WithTimeout(TimeSpan.FromSeconds(5));
```

3. Need to log the failure? Just do it!

```csharp
var loggedGetUserFlow = 
    timeoutGetUserFlow
      .DoOnFailure(ex => _logger.LogError(ex, "Failed to get user"));
```

4. I could go on, but you get the idea 😉

---

In short, with Flow you create components that are:
- Readable
- Predictable
- Reusable
- Easy to test

---

# Flow in Action: A Real-World Scenario

Let's walk through a realistic example of building and using a Flow.

### Step 1: 🏗️ Building the Core Business Logic

Say, we are the authors of `PaymentCollectionService`: 
* We want to generate and send payment collection notices.
* We've got to call several other services that we do not own.

Here is the complete method from our `PaymentCollectionService`. It defines the entire business process as a series of steps which are composed together.

```csharp
// This method lives in our PaymentCollectionService.
public IFlow<PostalTrackingId> CreateCollectionNoticeFlow(int userId)
{
    // 1️⃣
    return _billingService.GetBillingProfileFlow(userId)

        // 2️⃣
        .Select(profile => new { profile.Fullname, profile.BillingAddress })

        // 3️⃣
        .Chain(data =>
            _templateService.GenerateDocumentFlow(
                "CollectionNotice",
                data.Fullname,
                data.BillingAddress
            )
        )

        // 4️⃣
        .Chain(document => _dispatchService.SendByPostFlow(document));
}
```

Let's break it down line by line:
* **1️⃣:** It all starts by calling the billing service which returns a Flow to get a user's profile.
* **2️⃣:** `.Select()` takes the `profile` and extracts just the `Fullname` and `BillingAddress`.
* **3️⃣ & 4️⃣:** `.Chain()` is like saying "and then...". It links the next steps in the process, where each step can fail.

Our method returns a single, reusable `IFlow<PostalTrackingId>` that encapsulates our entire business process.

### Step 2: ✨ The Payoff - Enrichment at the Call-Site

Now, let's switch hats.

We are another team who is a consumer of the `PaymentCollectionService`.

The product requirements for our application demand strong resiliency for this feature.

And guess what!? 👉 We don't need to ask the `PaymentCollectionService` team to add retries or timeouts!

We can apply these policies ourselves 😎

---

First, we get the core Flow from `PaymentCollectionService`:
```csharp
var coreNoticeFlow = _paymentCollectionService.CreateCollectionNoticeFlow(userId: httpRequestParams.userId);
```

The external APIs can be flaky, so let's plug in the required resiliency policies:
```csharp
var resilientNoticeFlow = coreNoticeFlow
    .WithRetry(3)
    .WithTimeout(TimeSpan.FromSeconds(45));
```

And if it ultimately fails, we need to create a ticket for manual follow-up:

```csharp
var finalNoticeFlow = resilientNoticeFlow
    .DoOnFailure(ex => _ticketService.CreateManualFollowUpTicket("Collections", ex));
```

---

_We just saw the core principle of Flow in action:_

* _The `PaymentCollectionService` defined the business logic._
* _We, as the consumer, applied the operational logic on top._
* _The two are completely decoupled._

### Step 3: 🏁 Executing the Final, Enriched Flow

We've built our final recipe. We've **declared** our Flow/intention/plan of action. 

But NO actions have been taken yet - NOTHING has been executed.

Time to pass the recipe to the chef!

Enter FlowEngine. 

```csharp
var result = await FlowEngine.ExecuteAsync(finalNoticeFlow);

var message = result switch
{
    Success<PostalTrackingId> s => $"Notice sent! Tracking ID: {s.Value.Id}",
    Failure<PostalTrackingId> => "Failed to send collection notice after all retries.",
};

Console.WriteLine(message);
```

### 💡 Bottom Line 

Flow allows you to build clean and focused business logic.

You then compose operational concerns around it **where they're needed, not where they're defined**. 🎯


# 🧭 Intrigued!? Here's Your Learning Path!️

### Get Started Now (The 5-Minute Guide)

1.  **Start Here → [The Core Operators](./docs/CoreOperators.md)**

    A friendly introduction to the foundational primitives you'll be using to build your own Flows.

2.  **See More → [Practical Recipes](./docs/PracticalRecipes.md)**

    Ready for more? This document contains a collection of snippets for more advanced scenarios.

### Deeper Dive (For the Curious)

1.  **Go Pro → [Behaviours](./docs/Behaviours.md)**

    Ready to explore further? Learn how to extend your Flow with custom, reusable behaviours.


### Reference Material

1.  **[The "Why" → Design Rationale](./docs/DesignRationale.md)**: Curious about the principles behind the design? 

    This section explains the core architectural decisions that shape the library.

2.  **[API Blueprint](./docs/ApiBlueprint.cs):** A high-level map of the entire public API surface.
