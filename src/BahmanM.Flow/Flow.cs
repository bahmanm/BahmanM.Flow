namespace BahmanM.Flow;

public interface IFlow<T>
{
}

public static class Flow
{
    public static IFlow<T> Succeed<T>(T value) => new Ast.Primitive.Succeed<T>(value);

    public static IFlow<T> Fail<T>(Exception exception) => new Ast.Primitive.Fail<T>(exception);

    public static IFlow<T> Create<T>(Func<T> operation) => new Ast.Create.Sync<T>(() => operation());

    public static IFlow<T> Create<T>(Func<Task<T>> operation) => new Ast.Create.Async<T>(() => operation());

    public static IFlow<T[]> All<T>(IFlow<T> flow, params IFlow<T>[] moreFlows) =>
        new Ast.Primitive.All<T>([flow, ..moreFlows]);

    public static IFlow<T> Any<T>(IFlow<T> flow, params IFlow<T>[] moreFlows) =>
        new Ast.Primitive.Any<T>([flow, ..moreFlows]);

    public static IFlow<T> WithResource<TResource, T>(Func<TResource> acquire, Func<TResource, IFlow<T>> use)
        where TResource : IDisposable => new Ast.Resource.WithResource<TResource, T>(acquire, use);

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
            public delegate IFlow<T> Sync<T>(Exception error);
            public delegate Task<IFlow<T>> Async<T>(Exception error);
            public delegate Task<IFlow<T>> CancellableAsync<T>(Exception error, CancellationToken cancellationToken);
        }
    }
}
