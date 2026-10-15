namespace BahmanM.Flow.Description.Tests.Unit.Support;

internal static class DescriptionFlowBuilder
{
    internal static IDescription<T> Succeed<T>(T value) => new Primitive.Succeed<T>(value);

    internal static IDescription<T> Fail<T>(Exception exception) => new Primitive.Fail<T>(exception);

    internal static IDescription<T[]> All<T>(params IDescription<T>[] flows) =>
        new Primitive.All<T>(flows);

    internal static IDescription<T> Any<T>(params IDescription<T>[] flows) =>
        new Primitive.Any<T>(flows);

    internal static IDescription<T> Create<T>(Flow.Operations.Create.Sync<T> operation) =>
        new Create.Sync<T>(operation);

    internal static IDescription<T> Create<T>(Flow.Operations.Create.Async<T> operation) =>
        new Create.Async<T>(operation);

    internal static IDescription<T> Create<T>(Flow.Operations.Create.CancellableAsync<T> operation) =>
        new Create.CancellableAsync<T>(operation);

    internal static IDescription<T> Retry<T>(
        IDescription<T> source,
        int maxAttempts) =>
        new Behaviour.Retry.Retry<T>(source, maxAttempts, [typeof(TimeoutException)]);

    internal static IDescription<T> Retry<T>(
        IDescription<T> source,
        int maxAttempts,
        params Type[] nonRetryableExceptions) =>
        new Behaviour.Retry.Retry<T>(source, maxAttempts, nonRetryableExceptions);

    internal static IDescription<T> Timeout<T>(
        IDescription<T> source,
        TimeSpan duration) =>
        new Behaviour.Timeout.Timeout<T>(source, duration);

    internal static IDescription<TOut> Select<TIn, TOut>(
        IDescription<TIn> upstream,
        Flow.Operations.Select.Sync<TIn, TOut> operation) =>
        new Select.Sync<TIn, TOut>(upstream, operation);

    internal static IDescription<TOut> Select<TIn, TOut>(
        IDescription<TIn> upstream,
        Flow.Operations.Select.Async<TIn, TOut> operation) =>
        new Select.Async<TIn, TOut>(upstream, operation);

    internal static IDescription<TOut> Select<TIn, TOut>(
        IDescription<TIn> upstream,
        Flow.Operations.Select.CancellableAsync<TIn, TOut> operation) =>
        new Select.CancellableAsync<TIn, TOut>(upstream, operation);

    internal static IDescription<T> Validate<T>(
        IDescription<T> upstream,
        Func<T, bool> predicate,
        Func<T, Exception> exceptionFactory) =>
        new Validate.Sync<T>(upstream, predicate, exceptionFactory);

    internal static IDescription<T> Validate<T>(
        IDescription<T> upstream,
        Func<T, Task<bool>> predicate,
        Func<T, Exception> exceptionFactory) =>
        new Validate.Async<T>(upstream, predicate, exceptionFactory);

    internal static IDescription<T> Validate<T>(
        IDescription<T> upstream,
        Func<T, CancellationToken, Task<bool>> predicate,
        Func<T, Exception> exceptionFactory) =>
        new Validate.CancellableAsync<T>(upstream, predicate, exceptionFactory);

    internal static IDescription<T> DoOnSuccess<T>(
        IDescription<T> upstream,
        Flow.Operations.DoOnSuccess.Sync<T> action) =>
        new DoOnSuccess.Sync<T>(upstream, action);

    internal static IDescription<T> DoOnSuccess<T>(
        IDescription<T> upstream,
        Flow.Operations.DoOnSuccess.Async<T> action) =>
        new DoOnSuccess.Async<T>(upstream, action);

    internal static IDescription<T> DoOnSuccess<T>(
        IDescription<T> upstream,
        Flow.Operations.DoOnSuccess.CancellableAsync<T> action) =>
        new DoOnSuccess.CancellableAsync<T>(upstream, action);

    internal static IDescription<T> DoOnFailure<T>(
        IDescription<T> upstream,
        Flow.Operations.DoOnFailure.Sync action) =>
        new DoOnFailure.Sync<T>(upstream, action);

    internal static IDescription<T> DoOnFailure<T>(
        IDescription<T> upstream,
        Flow.Operations.DoOnFailure.Async action) =>
        new DoOnFailure.Async<T>(upstream, action);

    internal static IDescription<T> DoOnFailure<T>(
        IDescription<T> upstream,
        Flow.Operations.DoOnFailure.CancellableAsync action) =>
        new DoOnFailure.CancellableAsync<T>(upstream, action);

    internal static IDescription<TOut> Chain<TIn, TOut>(
        IDescription<TIn> upstream,
        Flow.Operations.Chain.Sync<TIn, TOut> operation) =>
        new Chain.Sync<TIn, TOut>(upstream, operation);

    internal static IDescription<TOut> Chain<TIn, TOut>(
        IDescription<TIn> upstream,
        Flow.Operations.Chain.Async<TIn, TOut> operation) =>
        new Chain.Async<TIn, TOut>(upstream, operation);

    internal static IDescription<TOut> Chain<TIn, TOut>(
        IDescription<TIn> upstream,
        Flow.Operations.Chain.CancellableAsync<TIn, TOut> operation) =>
        new Chain.CancellableAsync<TIn, TOut>(upstream, operation);

    internal static IDescription<T> Recover<T>(
        IDescription<T> source,
        Flow.Operations.Recover.Sync<T> recover) =>
        new Recover.Sync<T>(source, recover);

    internal static IDescription<T> Recover<T>(
        IDescription<T> source,
        Flow.Operations.Recover.Async<T> recover) =>
        new Recover.Async<T>(source, recover);

    internal static IDescription<T> Recover<T>(
        IDescription<T> source,
        Func<Exception, CancellationToken, Task<IFlow<T>>> recover) =>
        new Recover.CancellableAsync<T>(source, recover);

    internal static IDescription<T> WithResource<TResource, T>(
        Func<TResource> acquire,
        Func<TResource, IFlow<T>> use)
        where TResource : IDisposable =>
        new Resource.WithResource<TResource, T>(acquire, use);

    internal static IDescription<T> WithResource<TResource, T>(
        Func<Task<TResource>> acquireAsync,
        Func<TResource, IFlow<T>> use)
        where TResource : IAsyncDisposable =>
        new Resource.WithResourceAsync<TResource, T>(acquireAsync, use);

    internal static IDescription<T> WithResource<TResource, T>(
        Func<CancellationToken, Task<TResource>> acquireAsync,
        Func<TResource, IFlow<T>> use)
        where TResource : IAsyncDisposable =>
        new Resource.WithResourceCancellableAsync<TResource, T>(acquireAsync, use);
}
