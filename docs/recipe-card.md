# The Recipe Card: An API Quick Reference

```csharp
// --- The Core Types ---
public interface IFlow<T> { /* ... */ }
public abstract record Outcome<T>;
public sealed record Success<T>(T Value) : Outcome<T>;
public sealed record Failure<T>(Exception Exception) : Outcome<T>;


// --- 1. Starting a Flow ---
var a = Flow.Succeed(42);
var b = Flow.Fail<int>(new Exception("..."));
var c = Flow.Create(() => GetValueFromDatabase());      // Synchronous, failable
var d = Flow.Create(ct => GetValueFromApiAsync(ct));    // Asynchronous, cancellable


// --- 2. Composing a Flow ---
var initialFlow = Flow.Succeed(123);

var transformed = initialFlow.Select(i => i.ToString());      // T -> U
var sequenced   = initialFlow.Chain(i => GetNextFlow(i));      // T -> IFlow<U>
var validated   = initialFlow.Validate(i => i > 0, _ => new Exception("..."));
var recovered   = initialFlow.Recover(ex => GetFallbackFlow(ex));
var logged      = initialFlow.DoOnSuccess(i => Log(i));


// --- 3. Adding Resiliency ---
var resilient = initialFlow
    .WithRetry(3)
    .WithTimeout(TimeSpan.FromSeconds(5));


// --- 4. Executing the Flow ---
Outcome<string> outcome = await FlowEngine.ExecuteAsync(sequenced);


// --- 5. Handling the Result ---
string result = outcome switch
{
    Success<string> s => $"Got {s.Value}",
    Failure<string> f => $"Error: {f.Exception.Message}",
};
```
