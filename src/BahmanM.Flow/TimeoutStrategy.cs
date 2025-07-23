namespace BahmanM.Flow;

internal class TimeoutStrategy(TimeSpan duration) : IBehaviourStrategy
{
    public IFlow<T> ApplyTo<T>(SucceededNode<T> node) => node;
    public IFlow<T> ApplyTo<T>(FailedNode<T> node) => node;
    public IFlow<T> ApplyTo<T>(CreateNode<T> node) => node; // TODO: Implement sync timeout

    public IFlow<T> ApplyTo<T>(AsyncCreateNode<T> node)
    {
        Func<Task<T>> newOperation = () => node.Operation().WaitAsync(duration);
        return new AsyncCreateNode<T>(newOperation);
    }

    public IFlow<T> ApplyTo<T>(DoOnSuccessNode<T> node) => node;
    public IFlow<T> ApplyTo<T>(AsyncDoOnSuccessNode<T> node) => node;
    public IFlow<TOut> ApplyTo<TIn, TOut>(SelectNode<TIn, TOut> node) => node;
    public IFlow<TOut> ApplyTo<TIn, TOut>(AsyncSelectNode<TIn, TOut> node) => node;
    public IFlow<TOut> ApplyTo<TIn, TOut>(ChainNode<TIn, TOut> node) => node; // TODO: Implement sync timeout
    public IFlow<TOut> ApplyTo<TIn, TOut>(AsyncChainNode<TIn, TOut> node) => node; // TODO: Implement async timeout
}
