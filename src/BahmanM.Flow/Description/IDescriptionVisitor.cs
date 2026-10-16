namespace BahmanM.Flow.Description;

internal interface IDescriptionVisitor<TResult>
{
    TResult Visit<T>(Primitive.Succeed<T> node);
    TResult Visit<T>(Primitive.Fail<T> node);
    TResult Visit<T>(Primitive.All<T> node);
    TResult Visit<T>(Primitive.Any<T> node);
    TResult Visit<T>(Create.Sync<T> node);
    TResult Visit<T>(Create.Async<T> node);
    TResult Visit<T>(Create.CancellableAsync<T> node);
    TResult Visit<T>(Behaviour.Retry.Retry<T> node);
    TResult Visit<T>(Behaviour.Timeout.Timeout<T> node);
    TResult Visit<T>(Behaviour.Custom.Custom<T> node);
    TResult Visit<TIn, TOut>(Select.Sync<TIn, TOut> node);
    TResult Visit<TIn, TOut>(Select.Async<TIn, TOut> node);
    TResult Visit<TIn, TOut>(Select.CancellableAsync<TIn, TOut> node);
    TResult Visit<T>(Validate.Sync<T> node);
    TResult Visit<T>(Validate.Async<T> node);
    TResult Visit<T>(Validate.CancellableAsync<T> node);
    TResult Visit<T>(DoOnSuccess.Sync<T> node);
    TResult Visit<T>(DoOnSuccess.Async<T> node);
    TResult Visit<T>(DoOnSuccess.CancellableAsync<T> node);
    TResult Visit<T>(DoOnFailure.Sync<T> node);
    TResult Visit<T>(DoOnFailure.Async<T> node);
    TResult Visit<T>(DoOnFailure.CancellableAsync<T> node);
    TResult Visit<TIn, TOut>(Chain.Sync<TIn, TOut> node);
    TResult Visit<TIn, TOut>(Chain.Async<TIn, TOut> node);
    TResult Visit<TIn, TOut>(Chain.CancellableAsync<TIn, TOut> node);
    TResult Visit<T>(Recover.Sync<T> node);
    TResult Visit<T>(Recover.Async<T> node);
    TResult Visit<T>(Recover.CancellableAsync<T> node);
    TResult Visit<TResource, T>(Resource.WithResource<TResource, T> node) where TResource : IDisposable;
    TResult Visit<TResource, T>(Resource.WithResourceAsync<TResource, T> node) where TResource : IAsyncDisposable;
    TResult Visit<TResource, T>(Resource.WithResourceCancellableAsync<TResource, T> node) where TResource : IAsyncDisposable;
}
