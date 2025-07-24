namespace BahmanM.Flow;

public static class FlowExtensions
{
    public static IFlow<T> DoOnSuccess<T>(this IFlow<T> flow, Action<T> action) =>
        new DoOnSuccessNode<T>(flow, action);

    public static IFlow<T> DoOnSuccess<T>(this IFlow<T> flow, Func<T, Task> asyncAction) =>
        new AsyncDoOnSuccessNode<T>(flow, asyncAction);

    public static IFlow<TOut> Select<TIn, TOut>(this IFlow<TIn> flow, Func<TIn, TOut> operation) =>
        new SelectNode<TIn, TOut>(flow, operation);

    public static IFlow<TOut> Select<TIn, TOut>(this IFlow<TIn> flow, Func<TIn, Task<TOut>> asyncOperation) =>
        new AsyncSelectNode<TIn, TOut>(flow, asyncOperation);

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

    public static IFlow<T> WithBehaviour<T>(this IFlow<T> flow, IBehaviour<T> behaviour)
    {
        var strategy = new CustomBehaviourStrategy<T>(behaviour);
        return ((IFlowNode<T>)flow).Apply(strategy);
    }
}
