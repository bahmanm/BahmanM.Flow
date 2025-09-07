using BahmanM.Flow.Behaviour;
using BahmanM.Flow.Execution;
using BahmanM.Flow.Support;

namespace BahmanM.Flow;

public static class FlowExtensions
{
    public static IFlow<T> DoOnSuccess<T>(this IFlow<T> flow, Flow.Operations.DoOnSuccess.Sync<T> action) =>
        new Ast.DoOnSuccess.Sync<T>(flow, action);

    public static IFlow<T> DoOnSuccess<T>(this IFlow<T> flow, Flow.Operations.DoOnSuccess.Async<T> asyncAction) =>
        new Ast.DoOnSuccess.Async<T>(flow, asyncAction);

    public static IFlow<T> DoOnSuccess<T>(this IFlow<T> flow, Flow.Operations.DoOnSuccess.CancellableAsync<T> asyncAction) =>
        new Ast.DoOnSuccess.CancellableAsync<T>(flow, asyncAction);

    public static IFlow<T> DoOnFailure<T>(this IFlow<T> flow, Flow.Operations.DoOnFailure.Sync action) =>
        new Ast.DoOnFailure.Sync<T>(flow, action);

    public static IFlow<T> DoOnFailure<T>(this IFlow<T> flow, Flow.Operations.DoOnFailure.Async asyncAction) =>
        new Ast.DoOnFailure.Async<T>(flow, asyncAction);

    public static IFlow<T> DoOnFailure<T>(this IFlow<T> flow, Flow.Operations.DoOnFailure.CancellableAsync asyncAction) =>
        new Ast.DoOnFailure.CancellableAsync<T>(flow, asyncAction);

    public static IFlow<TOut> Select<TIn, TOut>(this IFlow<TIn> flow, Flow.Operations.Select.Sync<TIn, TOut> operation) =>
        new Ast.Select.Sync<TIn, TOut>(flow, operation);

    public static IFlow<TOut> Select<TIn, TOut>(this IFlow<TIn> flow, Flow.Operations.Select.Async<TIn, TOut> asyncOperation) =>
        new Ast.Select.Async<TIn, TOut>(flow, asyncOperation);

    public static IFlow<TOut> Select<TIn, TOut>(this IFlow<TIn> flow, Flow.Operations.Select.CancellableAsync<TIn, TOut> asyncOperation) =>
        new Ast.Select.CancellableAsync<TIn, TOut>(flow, asyncOperation);

    public static IFlow<TOut> Chain<TIn, TOut>(this IFlow<TIn> flow, Flow.Operations.Chain.Sync<TIn, TOut> operation) =>
        new Ast.Chain.Sync<TIn, TOut>(flow, operation);

    public static IFlow<TOut> Chain<TIn, TOut>(this IFlow<TIn> flow, Flow.Operations.Chain.Async<TIn, TOut> asyncOperation) =>
        new Ast.Chain.Async<TIn, TOut>(flow, asyncOperation);

    public static IFlow<TOut> Chain<TIn, TOut>(this IFlow<TIn> flow, Flow.Operations.Chain.CancellableAsync<TIn, TOut> asyncOperation) =>
        new Ast.Chain.CancellableAsync<TIn, TOut>(flow, asyncOperation);

    public static IFlow<T> Validate<T>(this IFlow<T> flow, Func<T, bool> predicate, Func<T, Exception> exceptionFactory) =>
        new Ast.Validate.Sync<T>(flow, predicate, exceptionFactory);

    public static IFlow<T> Validate<T>(this IFlow<T> flow, Func<T, Task<bool>> predicateAsync, Func<T, Exception> exceptionFactory) =>
        new Ast.Validate.Async<T>(flow, predicateAsync, exceptionFactory);

    public static IFlow<T> Validate<T>(this IFlow<T> flow, Func<T, CancellationToken, Task<bool>> predicateCancellableAsync, Func<T, Exception> exceptionFactory) =>
        new Ast.Validate.CancellableAsync<T>(flow, predicateCancellableAsync, exceptionFactory);

    public static IFlow<T> Recover<T>(this IFlow<T> flow, Flow.Operations.Recover.Sync<T> recover) =>
        new Ast.Recover.Sync<T>(flow, recover);

    public static IFlow<T> Recover<T>(this IFlow<T> flow, Flow.Operations.Recover.Async<T> recover) =>
        new Ast.Recover.Async<T>(flow, recover);

    public static IFlow<T> Recover<T>(this IFlow<T> flow, Func<Exception, CancellationToken, Task<IFlow<T>>> recover) =>
        new Ast.Recover.CancellableAsync<T>(flow, recover);

    public static IFlow<T> WithRetry<T>(this IFlow<T> flow, int maxAttempts, params Type[] nonRetryableExceptions)
    {
        var strategy = new RetryStrategy(maxAttempts, nonRetryableExceptions);
        return flow.AsNode().Apply(strategy);
    }

    public static IFlow<T> WithRetry<T>(this IFlow<T> flow, int maxAttempts)
    {
        return WithRetry(flow, maxAttempts, typeof(TimeoutException));
    }

    public static IFlow<T> WithTimeout<T>(this IFlow<T> flow, TimeSpan duration)
    {
        var strategy = new TimeoutStrategy(duration);
        return flow.AsNode().Apply(strategy);
    }

    public static IFlow<T> WithBehaviour<T>(this IFlow<T> flow, IBehaviour behaviour)
    {
        var strategy = new CustomBehaviourStrategy(behaviour);
        return flow.AsNode().Apply(strategy);
    }
}
