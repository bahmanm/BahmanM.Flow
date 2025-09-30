using BahmanM.Flow.Support;

namespace BahmanM.Flow.Behaviour;

internal class RetryStrategy(int maxAttempts, params Type[] nonRetryableExceptions) : IBehaviourStrategy
{
    private readonly int _maxAttempts = maxAttempts > 0
        ? maxAttempts
        : throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Max attempts must be a positive integer.");

    public IFlow<T> ApplyTo<T>(Ast.Primitive.Succeed<T> node) => node;
    public IFlow<T> ApplyTo<T>(Ast.Primitive.Fail<T> node) => node;

    public IFlow<T> ApplyTo<T>(Ast.DoOnSuccess.Sync<T> node) => node;
    public IFlow<T> ApplyTo<T>(Ast.DoOnSuccess.Async<T> node) => node;
    public IFlow<T> ApplyTo<T>(Ast.DoOnSuccess.CancellableAsync<T> node) => node;

    public IFlow<T> ApplyTo<T>(Ast.DoOnFailure.Sync<T> node) => node;
    public IFlow<T> ApplyTo<T>(Ast.DoOnFailure.Async<T> node) => node;
    public IFlow<T> ApplyTo<T>(Ast.DoOnFailure.CancellableAsync<T> node) => node;

    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Select.Sync<TIn, TOut> node) => node;
    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Select.Async<TIn, TOut> node) => node;
    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Select.CancellableAsync<TIn, TOut> node) => node;

    public IFlow<T> ApplyTo<T>(Ast.Recover.Sync<T> node) => node;
    public IFlow<T> ApplyTo<T>(Ast.Recover.Async<T> node) => node;
    public IFlow<T> ApplyTo<T>(Ast.Recover.CancellableAsync<T> node) => node;

    public IFlow<T[]> ApplyTo<T>(Ast.Primitive.All<T> node) => node;

    public IFlow<T> ApplyTo<T>(Ast.Primitive.Any<T> node) => node;

    public IFlow<T> ApplyTo<T>(Ast.Validate.Sync<T> node) => node;
    public IFlow<T> ApplyTo<T>(Ast.Validate.Async<T> node) => node;
    public IFlow<T> ApplyTo<T>(Ast.Validate.CancellableAsync<T> node) => node;

    public IFlow<T> ApplyTo<TResource, T>(Ast.Resource.WithResource<TResource, T> node) where TResource : IDisposable => node;
    public IFlow<T> ApplyTo<TResource, T>(Ast.Resource.WithResourceAsync<TResource, T> node) where TResource : IAsyncDisposable => node;
    public IFlow<T> ApplyTo<TResource, T>(Ast.Resource.WithResourceCancellableAsync<TResource, T> node) where TResource : IAsyncDisposable => node;

    public IFlow<T> ApplyTo<T>(Ast.Create.Sync<T> node)
    {
        Flow.Operations.Create.Sync<T> newOperation = () =>
        {
            Exception lastException = null!;
            for (var i = 0; i < _maxAttempts; i++)
            {
                try
                {
                    return node.Operation();
                }
                catch (Exception ex)
                {
                    if (nonRetryableExceptions.Contains(ex.GetType()))
                        throw;
                    lastException = ex;
                }
            }
            throw lastException!;
        };
        return new Ast.Create.Sync<T>(newOperation);
    }

    public IFlow<T> ApplyTo<T>(Ast.Create.Async<T> node)
    {
        Flow.Operations.Create.Async<T> newOperation = async () =>
        {
            Exception lastException = null!;
            for (var i = 0; i < _maxAttempts; i++)
            {
                try
                {
                    return await node.Operation();
                }
                catch (Exception ex)
                {
                    if (nonRetryableExceptions.Contains(ex.GetType()))
                        throw;
                    lastException = ex;
                }
            }
            throw lastException!;
        };
        return new Ast.Create.Async<T>(newOperation);
    }

    public IFlow<T> ApplyTo<T>(Ast.Create.CancellableAsync<T> node)
    {
        Func<CancellationToken, Task<T>> newOperation = async ct =>
        {
            Exception lastException = null!;
            for (var i = 0; i < _maxAttempts; i++)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    return await node.Operation(ct);
                }
                catch (Exception ex)
                {
                    if (nonRetryableExceptions.Contains(ex.GetType()))
                        throw;
                    lastException = ex;
                }
            }
            throw lastException!;
        };
        return new Ast.Create.CancellableAsync<T>(newOperation);
    }

    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Chain.Sync<TIn, TOut> node)
    {
        Flow.Operations.Chain.Sync<TIn,TOut> newOperation = (value) =>
            node.Operation(value).AsNode().Apply(this);
        return node with { Operation = newOperation };
    }

    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Chain.Async<TIn, TOut> node)
    {
        Flow.Operations.Chain.Async<TIn, TOut> newOperation = async (value) =>
            (await node.Operation(value)).AsNode().Apply(this);
        return node with { Operation = newOperation };
    }

    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Chain.CancellableAsync<TIn, TOut> node)
    {
        Flow.Operations.Chain.CancellableAsync<TIn, TOut> newOperation = async (value, cancellationToken) =>
            (await node.Operation(value, cancellationToken)).AsNode().Apply(this);
        return node with { Operation = newOperation };
    }
}
