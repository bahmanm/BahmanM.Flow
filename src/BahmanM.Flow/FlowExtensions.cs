namespace BahmanM.Flow;

public static class FlowExtensions
{
    public static IFlow<T> DoOnSuccess<T>(this IFlow<T> flow, Operations.DoOnSuccess.Sync<T> action) =>
        new DoOnSuccessNode<T>(flow, action);

    public static IFlow<T> DoOnSuccess<T>(this IFlow<T> flow, Operations.DoOnSuccess.Async<T> asyncAction) =>
        new AsyncDoOnSuccessNode<T>(flow, asyncAction);

    public static IFlow<T> DoOnSuccess<T>(this IFlow<T> flow, Operations.DoOnSuccess.CancellableAsync<T> asyncAction) =>
        new CancellableAsyncDoOnSuccessNode<T>(flow, asyncAction);

    public static IFlow<T> DoOnFailure<T>(this IFlow<T> flow, Action<Exception> action) =>
        new DoOnFailureNode<T>(flow, action);

    public static IFlow<T> DoOnFailure<T>(this IFlow<T> flow, Func<Exception, Task> asyncAction) =>
        new AsyncDoOnFailureNode<T>(flow, asyncAction);

    public static IFlow<TOut> Select<TIn, TOut>(this IFlow<TIn> flow, Operations.Select.Sync<TIn, TOut> operation) =>
        new SelectNode<TIn, TOut>(flow, operation);

    public static IFlow<TOut> Select<TIn, TOut>(this IFlow<TIn> flow, Operations.Select.Async<TIn, TOut> asyncOperation) =>
        new AsyncSelectNode<TIn, TOut>(flow, asyncOperation);

    public static IFlow<TOut> Select<TIn, TOut>(this IFlow<TIn> flow, Operations.Select.CancellableAsync<TIn, TOut> asyncOperation) =>
        new CancellableAsyncSelectNode<TIn, TOut>(flow, asyncOperation);

    public static IFlow<TOut> Chain<TIn, TOut>(this IFlow<TIn> flow, Func<TIn, IFlow<TOut>> operation) =>
        new ChainNode<TIn, TOut>(flow, operation);

    public static IFlow<TOut> Chain<TIn, TOut>(this IFlow<TIn> flow, Func<TIn, Task<IFlow<TOut>>> asyncOperation) =>
        new AsyncChainNode<TIn, TOut>(flow, asyncOperation);

    public static IFlow<T> WithRetry<T>(this IFlow<T> flow, int maxAttempts, params Type[] nonRetryableExceptions)
    {
        var strategy = new RetryStrategy(maxAttempts, nonRetryableExceptions);
        return ((IFlowNode<T>)flow).Apply(strategy);
    }
    
    public static IFlow<T> WithRetry<T>(this IFlow<T> flow, int maxAttempts)
    {
        return WithRetry(flow, maxAttempts, typeof(TimeoutException));
    }

    public static IFlow<T> WithTimeout<T>(this IFlow<T> flow, TimeSpan duration)
    {
        var strategy = new TimeoutStrategy(duration);
        return ((IFlowNode<T>)flow).Apply(strategy);
    }

    public static IFlow<T> WithBehaviour<T>(this IFlow<T> flow, IBehaviour behaviour)
    {
        var strategy = new CustomBehaviourStrategy(behaviour);
        return ((IFlowNode<T>)flow).Apply(strategy);
    }
}
