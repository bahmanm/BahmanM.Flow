namespace BahmanM.Flow;

public static class Operations
{
    public static class Create
    {
        public delegate T Sync<out T>();
        public delegate Task<T> Async<T>();
        public delegate Task<T> CancellableAsync<T>(CancellationToken cancellationToken);
    }

    public static class Select
    {
        public delegate TOut Sync<in TIn, out TOut>(TIn input);
        public delegate Task<TOut> Async<in TIn, TOut>(TIn input);
        public delegate Task<TOut> CancellableAsync<in TIn, TOut>(TIn input, CancellationToken cancellationToken);
    }

    public static class Chain
    {
        public delegate IFlow<TOut> Sync<in TIn, TOut>(TIn input);
        public delegate Task<IFlow<TOut>> Async<in TIn, TOut>(TIn input);
        public delegate Task<IFlow<TOut>> CancellableAsync<in TIn, TOut>(TIn input, CancellationToken cancellationToken);
    }

    public static class DoOnSuccess
    {
        public delegate void Sync<in T>(T input);
        public delegate Task Async<in T>(T input);
        public delegate Task CancellableAsync<in T>(T input, CancellationToken cancellationToken);
    }

    public static class DoOnFailure
    {
        public delegate void Sync(Exception error);
        public delegate Task Async(Exception error);
        public delegate Task CancellableAsync(Exception error, CancellationToken cancellationToken);
    }

    public static class Recover
    {
        public delegate T Sync<out T>(Exception error);
        public delegate Task<T> Async<T>(Exception error);
        public delegate Task<T> CancellableAsync<T>(Exception error, CancellationToken cancellationToken);
        
        public delegate IFlow<T> SyncWithFlow<T>(Exception error);
        public delegate Task<IFlow<T>> AsyncWithFlow<T>(Exception error);
        public delegate Task<IFlow<T>> CancellableAsyncWithFlow<T>(Exception error, CancellationToken cancellationToken);
    }
}
