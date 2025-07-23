namespace BahmanM.Flow;

internal class RetryStrategy(int maxAttempts) : IBehaviourStrategy
{
    private readonly int _maxAttempts = maxAttempts > 0
        ? maxAttempts
        : throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Max attempts must be a positive integer.");

    #region Pass-through Implementations

    public IFlow<T> ApplyTo<T>(SucceededNode<T> node) => node;
    public IFlow<T> ApplyTo<T>(FailedNode<T> node) => node;
    public IFlow<T> ApplyTo<T>(DoOnSuccessNode<T> node) => node;
    public IFlow<T> ApplyTo<T>(AsyncDoOnSuccessNode<T> node) => node;
    public IFlow<TOut> ApplyTo<TIn, TOut>(SelectNode<TIn, TOut> node) => node;
    public IFlow<TOut> ApplyTo<TIn, TOut>(AsyncSelectNode<TIn, TOut> node) => node;

    #endregion

    #region Rewriting Implementations
    public IFlow<T> ApplyTo<T>(CreateNode<T> node)
    {
        Func<T> newOperation = () =>
        {
            Exception lastException = null!;
            for (var i = 0; i < _maxAttempts; i++)
            {
                try { return node.Operation(); }
                catch (Exception ex) { lastException = ex; }
            }
            throw lastException!;
        };
        return new CreateNode<T>(newOperation);
    }

    public IFlow<TOut> ApplyTo<TIn, TOut>(ChainNode<TIn, TOut> node)
    {
        Func<TIn, IFlow<TOut>> newOperation = (value) =>
            ((IFlowNode<TOut>)node.Operation(value)).Apply(this);
        return node with { Operation = newOperation };
    }

    public IFlow<T> ApplyTo<T>(AsyncCreateNode<T> node)
    {
        Func<Task<T>> newOperation = async () =>
        {
            Exception lastException = null!;
            for (var i = 0; i < _maxAttempts; i++)
            {
                try { return await node.Operation(); }
                catch (Exception ex) { lastException = ex; }
            }
            throw lastException!;
        };
        return new AsyncCreateNode<T>(newOperation);
    }

    public IFlow<TOut> ApplyTo<TIn, TOut>(AsyncChainNode<TIn, TOut> node)
    {
        Func<TIn, Task<IFlow<TOut>>> newOperation = async (value) =>
            ((IFlowNode<TOut>)await node.Operation(value)).Apply(this);
        return node with { Operation = newOperation };
    }

    #endregion
}