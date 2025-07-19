namespace BahmanM.Flow;

public static class FlowExtensions
{
    public static IFlow<T> DoOnSuccess<T>(this IFlow<T> flow, Action<T> action) =>
        new DoOnSuccessFlow<T>(flow, action);

    public static IFlow<T> DoOnSuccess<T>(this IFlow<T> flow, Func<T, Task> asyncAction) =>
        new AsyncDoOnSuccessFlow<T>(flow, asyncAction);
}
