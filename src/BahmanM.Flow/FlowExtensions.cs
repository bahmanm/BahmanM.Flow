namespace BahmanM.Flow;

public static class FlowExtensions
{
    public static IFlow<T> DoOnSuccess<T>(this IFlow<T> flow, Action<T> action) =>
        new DoOnSuccessNode<T>(flow, action);

    public static IFlow<T> DoOnSuccess<T>(this IFlow<T> flow, Func<T, Task> asyncAction) =>
        new AsyncDoOnSuccessFlow<T>(flow, asyncAction);

    public static IFlow<TOut> Select<TIn, TOut>(this IFlow<TIn> flow, Func<TIn, TOut> operation) =>
        new SelectFlow<TIn, TOut>(flow, operation);

    public static IFlow<TOut> Select<TIn, TOut>(this IFlow<TIn> flow, Func<TIn, Task<TOut>> asyncOperation) =>
        new AsyncSelectFlow<TIn, TOut>(flow, asyncOperation);
}