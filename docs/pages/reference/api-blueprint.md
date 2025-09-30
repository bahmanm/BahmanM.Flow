# API Blueprint

Here's a high-level overview of the Flow's public API surface.

### Core Types

```csharp
public interface IFlow<T> { }

public abstract record Outcome<T>;

public sealed record Success<T>(T Value) : Outcome<T>;

public sealed record Failure<T>(Exception Exception) : Outcome<T>;
```

### Outcome Helpers

```csharp
public static class Outcome
{
    public static Outcome<T> Success<T>(T value);
    public static Outcome<T> Failure<T>(Exception exception);
}

public static class OutcomeExtensions
{
    public static bool IsSuccess<T>(this Outcome<T> outcome);
    public static bool IsFailure<T>(this Outcome<T> outcome);
    public static T GetOrElse<T>(this Outcome<T> outcome, T fallbackValue);
}
```

### Flow Factory

```csharp
public static class Flow
{
    // Creation
    public static IFlow<T> Succeed<T>(T value);
    public static IFlow<T> Fail<T>(Exception exception);
    public static IFlow<T> Create<T>(Func<T> operation);
    public static IFlow<T> Create<T>(Func<Task<T>> operation);
    public static IFlow<T> Create<T>(Func<CancellationToken, Task<T>> operation);

    // Concurrency
    public static IFlow<T[]> All<T>(IFlow<T> flow, params IFlow<T>[] moreFlows);
    public static IFlow<T> Any<T>(IFlow<T> flow, params IFlow<T>[] moreFlows);

    // Resources
    public static IFlow<T> WithResource<TResource, T>(Func<TResource> acquire, Func<TResource, IFlow<T>> use)
        where TResource : IDisposable;
    public static IFlow<T> WithResource<TResource, T>(Func<Task<TResource>> acquireAsync, Func<TResource, IFlow<T>> use)
        where TResource : IAsyncDisposable;
    public static IFlow<T> WithResource<TResource, T>(Func<CancellationToken, Task<TResource>> acquireAsync, Func<TResource, IFlow<T>> use)
        where TResource : IAsyncDisposable;
}
```

### Core Operators (Extensions)

```csharp
public static class FlowExtensions
{
    // Select (Transform)
    public static IFlow<TOut> Select<TIn, TOut>(this IFlow<TIn> flow, Func<TIn, TOut> operation);
    public static IFlow<TOut> Select<TIn, TOut>(this IFlow<TIn> flow, Func<TIn, Task<TOut>> asyncOperation);
    public static IFlow<TOut> Select<TIn, TOut>(this IFlow<TIn> flow, Func<TIn, CancellationToken, Task<TOut>> asyncOperation);

    // Chain (Sequence)
    public static IFlow<TOut> Chain<TIn, TOut>(this IFlow<TIn> flow, Func<TIn, IFlow<TOut>> operation);
    public static IFlow<TOut> Chain<TIn, TOut>(this IFlow<TIn> flow, Func<TIn, Task<IFlow<TOut>>> asyncOperation);
    public static IFlow<TOut> Chain<TIn, TOut>(this IFlow<TIn> flow, Func<TIn, CancellationToken, Task<IFlow<TOut>>> asyncOperation);

    // Validate
    public static IFlow<T> Validate<T>(this IFlow<T> flow, Func<T, bool> predicate, Func<T, Exception> exceptionFactory);
    public static IFlow<T> Validate<T>(this IFlow<T> flow, Func<T, Task<bool>> predicateAsync, Func<T, Exception> exceptionFactory);
    public static IFlow<T> Validate<T>(this IFlow<T> flow, Func<T, CancellationToken, Task<bool>> predicateCancellableAsync, Func<T, Exception> exceptionFactory);

    // Recover
    public static IFlow<T> Recover<T>(this IFlow<T> flow, Func<Exception, IFlow<T>> recover);
    public static IFlow<T> Recover<T>(this IFlow<T> flow, Func<Exception, Task<IFlow<T>>> recover);
    public static IFlow<T> Recover<T>(this IFlow<T> flow, Func<Exception, CancellationToken, Task<IFlow<T>>> recover);

    // Do (Side-effects)
    public static IFlow<T> DoOnSuccess<T>(this IFlow<T> flow, Action<T> action);
    public static IFlow<T> DoOnSuccess<T>(this IFlow<T> flow, Func<T, Task> asyncAction);
    public static IFlow<T> DoOnSuccess<T>(this IFlow<T> flow, Func<T, CancellationToken, Task> asyncAction);
    public static IFlow<T> DoOnFailure<T>(this IFlow<T> flow, Action<Exception> action);
    public static IFlow<T> DoOnFailure<T>(this IFlow<T> flow, Func<Exception, Task> asyncAction);
    public static IFlow<T> DoOnFailure<T>(this IFlow<T> flow, Func<Exception, CancellationToken, Task> asyncAction);
}
```

### Behaviours (Extensions)

```csharp
public static class FlowExtensions
{
    public static IFlow<T> WithRetry<T>(this IFlow<T> flow, int maxAttempts);
    public static IFlow<T> WithRetry<T>(this IFlow<T> flow, int maxAttempts, params Type[] nonRetryableExceptions);
    public static IFlow<T> WithTimeout<T>(this IFlow<T> flow, TimeSpan duration);
    public static IFlow<T> WithBehaviour<T>(this IFlow<T> flow, IBehaviour behaviour);
}
```

### Flow Engine

```csharp
public static class FlowEngine
{
    public static Task<Outcome<T>> ExecuteAsync<T>(IFlow<T> flow);
    public static Task<Outcome<T>> ExecuteAsync<T>(IFlow<T> flow, Options options);
}

public sealed record Options(CancellationToken CancellationToken);
```
