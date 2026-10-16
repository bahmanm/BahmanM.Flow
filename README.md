<p align="center">
  <img src="docs/assets/img/flow-1535x529.png" alt="Hydraulic Systems - Khuzestan, Iran"/>
  <small><i>Engineered Composable Flows (approx. 200 BCE) - Khuzestan, Iran</i></small>
</p>

---

<table>
  <tr>
    <td>
      <img src="docs/assets/img/flow-256x256.png" alt="Flow Logo"/>
    </td>
    <td align="center">
        <h1>Flow</h1>
        <h3>Lightweight Library for Clean and Composable Business Logic</h3>
        <img src="https://github.com/bahmanm/BahmanM.Flow/actions/workflows/ci.yml/badge.svg" alt="CI"/>
        <img src="https://img.shields.io/nuget/v/BahmanM.Flow?style=flat&logo=nuget&label=NuGet" alt="NuGet Version"/>
        <a href="https://bahmanm.github.io/BahmanM.Flow/"><img src="https://img.shields.io/badge/documentation-website-blue" alt="Documentation"/></a>
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
      💡 WHAT IF you could write code as a clean, chainable sequence of operations instead?<br/>
      💡 A sequence which clearly separates the "happy path" from error handling, logging, retries, ...<br/>
      💡 A sequence which is a pleasure to express, read, and maintain?
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
  <tr>
    <td>🧠 <i>Why!?</i></td>
    <td> 
      👉 Business Logic is<br/>
      &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;➖ Not just a procedure. No!<br/>
      &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;➕ A reusable <b>recipe</b> (the Flow) executed by a <b>chef</b> (the FlowEngine).<br/> 
      👉 Decoupling of <i>declaration</i> from <i>execution</i> makes your code<br/>
      &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;➕ Testable<br/>
      &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;➕ Maintainable<br/>
      &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;➕ Extensible<br/>
    </td>
  </tr>
</table>

---

# ⏳ Flow in 60 Seconds

1️⃣ Imagine turning this imperative code:

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

2️⃣ Into this Flow:

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

3️⃣ Here's a quick glance at what happened above:

<table>
  <tr>
    <td>Exceptions</td><td>➡️</td><td>Data</td>
    <td>Operators (e.g. <code>Chain</code>) can throw. Flow captures them and returns <code>Failure</code> - no manual try‑catch anymore.</td>
  </tr>
  <tr>
    <td>Guards</td><td>➡️</td><td>Declarative</td>
    <td><code>Validate</code> encodes the pre/post-conditions. When false, the flow turns into <code>Failure</code> with the exception you choose.</td>
  </tr>
  <tr>
    <td>Side-Effects</td><td>➡️</td><td>Contained</td>
    <td><code>DoOnSuccess</code> changes the outcome to a failure if its callback throws. <code>DoOnFailure</code> suppresses callback exceptions and preserves the original failure.</td>
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

---

# 🍳 The Core Recipe in 30 Seconds

If you'd rather just dive deep right in, this section is all you'd need!

The main thing you need to remember is that Flow is built around a small, composable set of types and methods.

Here are the core building blocks. Have fun!

```csharp
// --- 0️⃣ The Core Types ---
public interface IFlow<T> { /* ... */ }
public abstract record Outcome<T>;
public sealed record Success<T>(T Value) : Outcome<T>;
public sealed record Failure<T>(Exception Exception) : Outcome<T>;


// --- 1️⃣ Starting a Flow ---
var a = Flow.Succeed(42);
var b = Flow.Fail<int>(new Exception("..."));
var c = Flow.Create(() => GetValueFromDatabase());      // Synchronous, failable
var d = Flow.Create(ct => GetValueFromApiAsync(ct));    // Asynchronous, cancellable


// --- 2️⃣ Composing Flows and Operations ---
var initialFlow = Flow.Succeed(123);

var transformed = initialFlow.Select(i => i.ToString());       // T -> U
var sequenced   = initialFlow.Chain(i => GetNextFlow(i));      // T -> IFlow<U>
var validated   = initialFlow.Validate(i => i > 0,
                                       _ => new Exception("..."));
var recovered   = initialFlow.Recover(ex => GetFallbackFlow(ex));
var logged      = initialFlow.DoOnSuccess(i => Log(i));


// --- 3️⃣ Adding Resiliency ---
var resilient = initialFlow
    .WithRetry(3)
    .WithTimeout(TimeSpan.FromSeconds(5));


// --- 4️⃣ Executing the Flow ---
Outcome<string> outcome = await FlowEngine.ExecuteAsync(sequenced);


// --- 5️⃣ Handling the Result ---
string result = outcome switch
{
    Success<string> s => $"Got {s.Value}",
    Failure<string> f => $"Error: {f.Exception.Message}",
};
```

**Want to see this in a complete, runnable project? Check out the ["Should I Go Outside?" example application](./examples/ShouldIGoOutside).**

---

# 🧩 Flow is Composable.  _Wait...What!?_

The previous example was cool: clean and declarative. But the REAL win is in Flow's **plug-and-play design** 🔌

-  A Flow is just a **recipe** for your business logic.
-  Since it is nothing more than a definition, it can be enriched and reused: cheap and simple.
-  You can enhance any Flow with new behaviours and operators without ever touching the original code - no, seriously 😎

### Let's Break it Down

Assume there's this flow which sends a notification to a user. Oh, and you do **not** own the code.

1️⃣ Say, you need a retry logic? Easy - you simply enrich your existing flow!

```csharp
var resilientGetUserFlow = 
    GetUserAndNotifyFlow(httpRequestParams.userId)
      .WithRetry(3);
```

2️⃣ Maybe you want to add a timeout, too? No problem!

```csharp
var timeoutGetUserFlow = 
    resilientGetUserFlow 
      .WithTimeout(TimeSpan.FromSeconds(5));
```

3️⃣ How about logging the failure? Just do it!

```csharp
var loggedGetUserFlow = 
    timeoutGetUserFlow
      .DoOnFailure(ex => _logger.LogError(ex, "Failed to get user"));
```

4️⃣ I could go on, but you get the idea 😉

### The Gist

A Flow is a **recipe** which is:

✅ **Composable**: Mix and match from various services and libraries.<br/>
✅ **Enrichable**: At the call-site/client-side; add new behaviors at the last second, without ever touching the upstream code.

---

# ![Flow Logo](docs/assets/img/flow-32x32.png) Install

Requires a .NET 10 (`net10.0`) project.

-  .NET CLI: `dotnet add package BahmanM.Flow`
-  PackageReference: `<PackageReference Include="BahmanM.Flow" Version="x.y.z" />`
-  NuGet page: https://www.nuget.org/packages/BahmanM.Flow/

---

# ⚙️ Flow in Action: A Real-World Composition

Let’s look at a more involved example. The goal is to see how Flow allows us to **compose** complex opoerations from smaller, independent pieces.

Say, we're writing a Kafka consumer which receives a `DispatchRequestedMessage`, looks up the order, fetches a shipping rate, recovers to a safe default on 404, transforms to a dispatch message, and publishes it to another topic.

_Note: Admittedly, this is not a production-grade code. I've made quite a few assumptions to keep the snippet fit the README._

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
                        .GetShippingRateFlow(order.ShipTo)
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

<table>
  <tr>
    <td>🧠 Compose Behaviours and Operations</td>
    <td>
      Where they are need, not where they are defined.<br/>
      Policies (timeout, retry) and whole‑flow branching are configured in our code and not in the upstream modules.
    </td>
  </tr>
  <tr>
    <td>🧠 Value Introspection</td>
    <td><code>Validate(order is not null ...)</code> encodes a business invariant right in the Flow.</td>
  </tr>
  <tr>
    <td>🧠 Branching</td>
    <td><code>Recover</code> swaps the entire downstream when the carrier returns 404 (fallback to default rate).</td>
  </tr>
</table>

---

# 💡 Intrigued!?

You've had the whirlwind tour!

For the full story, including practical recipes, deep-dive articles, and the complete API reference, head over to [🏡 Flow's Home](https://flow.bahmanm.com/).
