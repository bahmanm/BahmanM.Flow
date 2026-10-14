namespace BahmanM.Flow.Description.Tests.Unit.Support;

internal static class DescriptionFlowBuilder
{
    internal static Description.IDescription<T> Succeed<T>(T value) => new Description.Primitive.Succeed<T>(value);

    internal static Description.IDescription<T> Fail<T>(Exception exception) => new Description.Primitive.Fail<T>(exception);

    internal static Description.IDescription<T[]> All<T>(params Description.IDescription<T>[] flows) =>
        new Description.Primitive.All<T>(flows);

    internal static Description.IDescription<T> Create<T>(Flow.Operations.Create.Sync<T> operation) =>
        new Description.Create.Sync<T>(operation);

    internal static Description.IDescription<T> Create<T>(Flow.Operations.Create.Async<T> operation) =>
        new Description.Create.Async<T>(operation);

    internal static Description.IDescription<T> Create<T>(Flow.Operations.Create.CancellableAsync<T> operation) =>
        new Description.Create.CancellableAsync<T>(operation);

    internal static Description.IDescription<TOut> Select<TIn, TOut>(
        Description.IDescription<TIn> upstream,
        Flow.Operations.Select.Sync<TIn, TOut> operation) =>
        new Description.Select.Sync<TIn, TOut>(upstream, operation);

    internal static Description.IDescription<TOut> Select<TIn, TOut>(
        Description.IDescription<TIn> upstream,
        Flow.Operations.Select.Async<TIn, TOut> operation) =>
        new Description.Select.Async<TIn, TOut>(upstream, operation);

    internal static Description.IDescription<TOut> Select<TIn, TOut>(
        Description.IDescription<TIn> upstream,
        Flow.Operations.Select.CancellableAsync<TIn, TOut> operation) =>
        new Description.Select.CancellableAsync<TIn, TOut>(upstream, operation);

    internal static Description.IDescription<T> Validate<T>(
        Description.IDescription<T> upstream,
        Func<T, bool> predicate,
        Func<T, Exception> exceptionFactory) =>
        new Description.Validate.Sync<T>(upstream, predicate, exceptionFactory);

    internal static Description.IDescription<T> Validate<T>(
        Description.IDescription<T> upstream,
        Func<T, Task<bool>> predicate,
        Func<T, Exception> exceptionFactory) =>
        new Description.Validate.Async<T>(upstream, predicate, exceptionFactory);

    internal static Description.IDescription<T> Validate<T>(
        Description.IDescription<T> upstream,
        Func<T, CancellationToken, Task<bool>> predicate,
        Func<T, Exception> exceptionFactory) =>
        new Description.Validate.CancellableAsync<T>(upstream, predicate, exceptionFactory);

    internal static Description.IDescription<T> DoOnSuccess<T>(
        Description.IDescription<T> upstream,
        Flow.Operations.DoOnSuccess.Sync<T> action) =>
        new Description.DoOnSuccess.Sync<T>(upstream, action);

    internal static Description.IDescription<T> DoOnSuccess<T>(
        Description.IDescription<T> upstream,
        Flow.Operations.DoOnSuccess.Async<T> action) =>
        new Description.DoOnSuccess.Async<T>(upstream, action);

    internal static Description.IDescription<T> DoOnSuccess<T>(
        Description.IDescription<T> upstream,
        Flow.Operations.DoOnSuccess.CancellableAsync<T> action) =>
        new Description.DoOnSuccess.CancellableAsync<T>(upstream, action);

    internal static Description.IDescription<T> DoOnFailure<T>(
        Description.IDescription<T> upstream,
        Flow.Operations.DoOnFailure.Sync action) =>
        new Description.DoOnFailure.Sync<T>(upstream, action);

    internal static Description.IDescription<T> DoOnFailure<T>(
        Description.IDescription<T> upstream,
        Flow.Operations.DoOnFailure.Async action) =>
        new Description.DoOnFailure.Async<T>(upstream, action);

    internal static Description.IDescription<T> DoOnFailure<T>(
        Description.IDescription<T> upstream,
        Flow.Operations.DoOnFailure.CancellableAsync action) =>
        new Description.DoOnFailure.CancellableAsync<T>(upstream, action);

    internal static Description.IDescription<TOut> Chain<TIn, TOut>(
        Description.IDescription<TIn> upstream,
        Flow.Operations.Chain.Sync<TIn, TOut> operation) =>
        new Description.Chain.Sync<TIn, TOut>(upstream, operation);

    internal static Description.IDescription<TOut> Chain<TIn, TOut>(
        Description.IDescription<TIn> upstream,
        Flow.Operations.Chain.Async<TIn, TOut> operation) =>
        new Description.Chain.Async<TIn, TOut>(upstream, operation);

    internal static Description.IDescription<TOut> Chain<TIn, TOut>(
        Description.IDescription<TIn> upstream,
        Flow.Operations.Chain.CancellableAsync<TIn, TOut> operation) =>
        new Description.Chain.CancellableAsync<TIn, TOut>(upstream, operation);

    internal static Description.IDescription<T> Recover<T>(
        Description.IDescription<T> source,
        Flow.Operations.Recover.Sync<T> recover) =>
        new Description.Recover.Sync<T>(source, recover);

    internal static Description.IDescription<T> Recover<T>(
        Description.IDescription<T> source,
        Flow.Operations.Recover.Async<T> recover) =>
        new Description.Recover.Async<T>(source, recover);

    internal static Description.IDescription<T> Recover<T>(
        Description.IDescription<T> source,
        Func<Exception, CancellationToken, Task<IFlow<T>>> recover) =>
        new Description.Recover.CancellableAsync<T>(source, recover);
}
