namespace BahmanM.Flow.Shared;

internal static class Operations
{
    internal static class Create
    {
        internal delegate T Sync<out T>();
        internal delegate Task<T> Async<T>();
        internal delegate Task<T> CancellableAsync<T>(CancellationToken cancellationToken);
    }

    internal static class Select
    {
        internal delegate TOut Sync<in TIn, out TOut>(TIn input);
        internal delegate Task<TOut> Async<in TIn, TOut>(TIn input);
        internal delegate Task<TOut> CancellableAsync<in TIn, TOut>(TIn input, CancellationToken cancellationToken);
    }

    internal static class Chain
    {
        internal delegate IFlow<TOut> Sync<in TIn, TOut>(TIn input);
        internal delegate Task<IFlow<TOut>> Async<in TIn, TOut>(TIn input);
        internal delegate Task<IFlow<TOut>> CancellableAsync<in TIn, TOut>(TIn input, CancellationToken cancellationToken);
    }

    internal static class DoOnSuccess
    {
        internal delegate void Sync<in T>(T input);
        internal delegate Task Async<in T>(T input);
        internal delegate Task CancellableAsync<in T>(T input, CancellationToken cancellationToken);
    }

    internal static class DoOnFailure
    {
        internal delegate void Sync(Exception error);
        internal delegate Task Async(Exception error);
        internal delegate Task CancellableAsync(Exception error, CancellationToken cancellationToken);
    }

    internal static class Recover
    {
        internal delegate IFlow<T> Sync<T>(Exception error);
        internal delegate Task<IFlow<T>> Async<T>(Exception error);
        internal delegate Task<IFlow<T>> CancellableAsync<T>(Exception error, CancellationToken cancellationToken);
    }
}
