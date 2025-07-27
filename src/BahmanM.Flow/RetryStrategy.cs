namespace BahmanM.Flow;

internal class RetryStrategy(int maxAttempts, params Type[] nonRetryableExceptions) : IBehaviourStrategy
{
    private readonly Type[] _nonRetryableExceptions;
    private readonly int _maxAttempts;

    public RetryStrategy(int maxAttempts, params Type[] nonRetryableExceptions)
    {
        _nonRetryableExceptions = nonRetryableExceptions;
        _maxAttempts = maxAttempts > 0
            ? maxAttempts
            : throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Max attempts must be a positive integer.");
    }

    public RetryStrategy(int maxAttempts) : this(maxAttempts, [typeof(TimeoutException)])
    {
    }

    #region Pass-through Implementations

    public IFlow<T> ApplyTo<T>(Ast.Primitive.Succeed<T> node) => node;
    public IFlow<T> ApplyTo<T>(Ast.Primitive.Fail<T> node) => node;
    public IFlow<T> ApplyTo<T>(Ast.Create.CancellableAsync<T> node)
    {
        throw new NotImplementedException();
    }

    public IFlow<T> ApplyTo<T>(Ast.DoOnSuccess.Sync<T> node) => node;
    public IFlow<T> ApplyTo<T>(Ast.DoOnSuccess.Async<T> node) => node;
    public IFlow<T> ApplyTo<T>(Ast.DoOnSuccess.CancellableAsync<T> node)
    {
        throw new NotImplementedException();
    }

    public IFlow<T> ApplyTo<T>(Ast.DoOnFailure.Sync<T> node) => node;
    public IFlow<T> ApplyTo<T>(Ast.DoOnFailure.Async<T> node) => node;
    public IFlow<T> ApplyTo<T>(Ast.DoOnFailure.CancellableAsync<T> node) => node;

    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Select.Sync<TIn, TOut> node) => node;
    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Select.Async<TIn, TOut> node) => node;
    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Select.CancellableAsync<TIn, TOut> node) => node;

    #endregion

    #region Rewriting Implementations

    public IFlow<T> ApplyTo<T>(Ast.Create.Sync<T> node)
    {
        Operations.Create.Sync<T> newOperation = () =>
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
                    if (_nonRetryableExceptions.Contains(ex.GetType()))
                        throw;
                    lastException = ex;
                }
            }
            throw lastException!;
        };
        return new Ast.Create.Sync<T>(newOperation);
    }

    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Chain.Sync<TIn, TOut> node)
    {
        Operations.Chain.Sync<TIn,TOut> newOperation = (value) =>
            ((Ast.INode<TOut>)node.Operation(value)).Apply(this);
        return node with { Operation = newOperation };
    }

    public IFlow<T> ApplyTo<T>(Ast.Create.Async<T> node)
    {
        Operations.Create.Async<T> newOperation = async () =>
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
                    if (_nonRetryableExceptions.Contains(ex.GetType()))
                        throw;
                    lastException = ex;
                }
            }
            throw lastException!;
        };
        return new Ast.Create.Async<T>(newOperation);
    }

    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Chain.Async<TIn, TOut> node)
    {
        Operations.Chain.Async<TIn, TOut> newOperation = async (value) =>
            ((Ast.INode<TOut>)await node.Operation(value)).Apply(this);
        return node with { Operation = newOperation };
    }

    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Chain.CancellableAsync<TIn, TOut> node)
    {
        Operations.Chain.CancellableAsync<TIn, TOut> newOperation = async (value, cancellationToken) =>
            ((Ast.INode<TOut>)await node.Operation(value, cancellationToken)).Apply(this);
        return node with { Operation = newOperation };
    }

    public IFlow<T[]> ApplyTo<T>(Ast.Primitive.All<T> node) => node;

    public IFlow<T> ApplyTo<T>(Ast.Primitive.Any<T> node) => node;

    #endregion
}
