<p align="center">
  <img src="docs/imgs/flow-1535x529.png" alt="Hydraulic Systems - Khuzestan, Iran"/>
  <small><i>Hydraulic Systems - Khuzestan, Iran</i></small>
</p>

---

<table style="width:100%;">
  <tr>
    <td>
      <img src="docs/imgs/flow-256x256.png" alt="Flow Logo"/>
    </td>
    <td align="center">
        <h1>Flow</h1>
        <h2>Clean and Composable Business Logic</h2>
        <img src="https://img.shields.io/nuget/v/BahmanM.Flow?style=flat&logo=nuget&label=NuGet" alt="NuGet Version"/>  
        <img src="https://github.com/bahmanm/BahmanM.Flow/actions/workflows/ci.yml/badge.svg" alt="CI"/>  
        <img src="https://app.fossa.com/api/projects/git%2Bgithub.com%2Fbahmanm%2FBahmanM.Flow.svg?type=shield" alt="FOSSA"/>  
    </td>
  </tr>
  <tr>
    <td>😣 <i>Ugh.</i></td>
    <td>
      ❌ Is your business logic a tangled, and potentially ugly, mess?<br/>
      ❌ Are there `try-catch` blocks and `if-else` statements everywhere?<br/>
      ❌ Do you see side effects, error handling, logging, retries, and more all over the place?
    </td>
  </tr>
  <tr>
    <td>🤔 <i>Oh!?</i></td>
    <td>
      💡 WHAT IF you could build your workflow as a clean, chainable pipeline of operations instead?<br/>
      💡 A pipeline which clearly separates the "happy path" from error handling, logging, retries, ...<br/>
      💡 A pipeline which is a pleasure to express, read, and maintain?
    </td>
  </tr>
  <tr>
    <td>😌 <i>Flow!</i></td>
    <td> 
      ✅ Lightweight<br/>
      ✅ Fluent<br/>
      &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;✅ Declarative<br/>
      &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;✅ Composable<br/>
      &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;✅ Testable<br/>
      ✅ Resilient<br/>
      ✅ Observable
    </td>
  </tr>
</table>

---

# ⏳ Flow in 60 Seconds

Imagine turning this imperative code:

```csharp
public async Task<Guid> SendWelcomeAsync(int userId)
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

    EmailMessage email;
    try
    {
        email = user.IsVip
            ? Templates.VipWelcomeEmailFor(user)
            : Templates.StandardWelcomeEmailFor(user);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Templating failed for {UserId}", userId);
        _metrics.Increment("emails.template.exceptions");
        throw;
    }

    try
    {
        var messageId = await _emails.SendAsync(email);
        _metrics.Increment("emails.sent");
        return messageId;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Send failed for {UserId}", userId);
        _metrics.Increment("emails.send.failures");
        throw;
    }
}
```

Into this Flow:

```csharp
var onboardingFlow =
    Flow.Succeed(userId)
        .Chain(Users.FindUserFlow)
        .Validate(
            user => user is not null, 
            _ => new NotFoundException($"{userId}"))
        .Chain(user => 
            switch (user.IsVip) {
              true =>  Flow.Succeed(Templates.VipWelcomeEmailFor(user))
              false => Flow.Succeed(Templates.StandardWelcomeEmailFor(user))
            })
        .Chain(Emails.SendWelcomeEmailFlow)
        .DoOnSuccess(_ => 
            Metrics.Increment("emails.sent"))
        .DoOnFailure(ex => 
            Logger.LogError(ex, "Send failed"));

await FlowEngine.ExecuteAsync(onboardingFlow);
```

Here's a quick glance at what happend above:

<table>
  <tr>
    <td>Exceptions</td><td>➡️</td><td>Data</td>
    <td>Operators (e.g. <code>Chain</code>) can throw. Flow captures them and returns <code>Failure</code> - no manual try‑catch anymore.</td>
  </tr>
  <tr>
    <td>Guards</td><td>➡️</td><td>Declaraitve</td>
    <td><code>Validate</code> encodes the pre/post-conditions. When false, the flow turns into <code>Failure</code> with the exception you choose.</td>
  </tr>
  <tr>
    <td>Side-Effects</td><td>➡️</td><td>Contained</td>
    <td><code>DoOnFailure</code>/<code>DoOnSuccess</code> log/measure without changing outcomes - they cannot control the flow anymore.</td>
  </tr>
  <tr>
    <td>Alternatives</td><td>➡️</td><td>Explicit</td>
    <td><code>Recover</code> can branch the whole flow on specific errors.</td>
  </tr>
  <tr>
    <td>Errors</td><td>➡️</td><td>Unswallowed</td>
    <td>If you don't 'recover', <code>ExecuteAsync</code> returns <code>Failure</code> with the original exception.</td>
  </tr>
</table>

But...the REAL win is in Flow's **plug-and-play design** 🔌
-  A Flow is just a **recipe** for your business logic.
-  Since it is nothing more than a definition, it can be enriched and reused: cheap and simple.
-  You can enhance any Flow with new behaviours without ever touching the original code - no, seriously 😎

---

Allow me to demonstrate:

1. Say, next sprint, you realise you need a retry logic? Easy - you simply enrich your existing flow!

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
-  Readable
-  Predictable
-  Reusable
-  Easy to test

---

# ![Flow Logo](docs/imgs/flow-32x32.png) Install

-  .NET CLI: `dotnet add package BahmanM.Flow`
-  PackageReference: `<PackageReference Include="BahmanM.Flow" Version="x.y.z" />`
-  NuGet page: https://www.nuget.org/packages/BahmanM.Flow/

---

# ⚙️ Flow in Action: Order Dispatch Pipeline

Let’s walk through a realistic dispatcher. The upstream modules expose raw, policy‑free Flows; the consumer composes and adds tiny operational policy at the edge.

### Step 1: Consumer orchestrates the recipe

We receive a `DispatchRequestedMessage`, look up the order, fetch a shipping rate, recover to a safe default on 404, transform to a dispatch message, and publish.

Here is the complete flow in the consumer. Note how policies (timeout, retry) and whole‑flow branching live here, not in upstream modules.

```csharp
class DispatchRequestedConsumer : IKafkaConsumer
{
    async Task Consume(DispatchRequestedMessage message)
    {
        var consumeFlow =
            Flow.Succeed(message)
                .Select(_adapters.AsOrderId)
                .Chain(orderId => 
                    _orders.FindOrderFlow(orderId))
                .Validate(
                    order => order is not null,
                    _ => new NotFoundException("Order not found"))
                .DoOnFailure(ex => 
                    _logger.LogWarning($"Order lookup failed: {ex.Message}"))
                .Chain(order => 
                    _rates
                        .GetShippingRateFlow(order!.ShipTo)
                        .WithTimeout(TimeSpan.FromSeconds(5))
                        .WithRetry(3)
                        .Recover(ex => 
                            ex is HttpNotFoundException 
                               ? Flow.Succeed(ShippingRate.StandardFallback)
                               : Flow.Fail<ShippingRate>(ex))
                        .Select(rate => 
                            (order, rate)))
                .Select(x => 
                    _adapters.AsDispatchMessage(x.order, x.rate))
                .Chain(dispatchMessage => 
                    _producer.ProduceFlow(dispatchMessage))
                .DoOnFailure(ex => 
                    _logger.LogError(ex, "Produce failed"))
                .DoOnSuccess(_ => 
                    _otel.IncreaseCounter("shipping.dispatch.produced"));

        await FlowEngine.ExecuteAsync(consumeFlow);
    }
}
```

Why this matters:
- Value‑introspective gate: `Validate(order is not null, …)` encodes a business truth.
- Flow‑level branching: `Recover` swaps the entire downstream when the carrier returns 404 (safe default rate).
- Behaviour ordering is semantic: `WithTimeout` then `WithRetry` gives a single end‑to‑end budget for all attempts.

### Step 2: Upstream modules expose raw, reusable Flows

Keep these policy‑free so they compose cleanly at the call‑site.

```csharp
public sealed class OrdersRepository(DbContext _db)
{
    public IFlow<Order?> FindOrderFlow(OrderId orderId) =>
        Flow.Create(() => _db.Orders.FindById(orderId));
}

public sealed class CarrierRatesClient(IHttpClient _http)
{
    public IFlow<ShippingRate> GetShippingRateFlow(Address shipTo) =>
        Flow.Create<ShippingRate>(async ct =>
        {
            var url = Routes.Rates.ForDestination(shipTo);
            return await _http.GetJsonAsync<ShippingRate>(url, ct); // throws HttpNotFoundException on 404
        });
}

public sealed class DispatchTopicProducer(IMessageBus _bus)
{
    public IFlow<Guid> ProduceFlow(DispatchMessage message) =>
        Flow.Create(async () => await _bus.PublishAsync("shipping.dispatch", message));
}

public sealed class Adapters
{
    public OrderId ToOrderId(DispatchRequestedMessage m) => new(m.OrderId);
    public DispatchMessage ToDispatchMessage(Order order, ShippingRate rate) => new(order.Id, order.ShipTo, rate);
}
```

### Step 3: Execute the final recipe

We’ve declared our recipe; now pass it to the chef — the `FlowEngine`.

```csharp
await FlowEngine.ExecuteAsync(consumeFlow);
```

### 💡 Bottom line

Write clean, focused business logic as an immutable recipe.
Compose operational concerns **where they’re needed, not where they’re defined**. 🎯

---

# 💡 Intrigued!?

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

2.  **[API Blueprint](./docs/ApiBlueprint.cs)**: A high-level map of the entire public API surface.
