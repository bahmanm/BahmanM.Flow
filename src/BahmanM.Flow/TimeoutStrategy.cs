namespace BahmanM.Flow;

internal class TimeoutStrategy(TimeSpan duration) : IBehaviourStrategy
{
    public IFlow<T> ApplyTo<T>(SucceededNode<T> node) => node;
    public IFlow<T> ApplyTo<T>(FailedNode<T> node) => node;

    public IFlow<T> ApplyTo<T>(CreateNode<T> node)
    {
        Func<Task<T>> newOperation = () => Task.Run(node.Operation).WaitAsync(duration);
        return new AsyncCreateNode<T>(newOperation);
    }

    public IFlow<T> ApplyTo<T>(AsyncCreateNode<T> node)
    {
        Func<Task<T>> newOperation = async () => await node.Operation().WaitAsync(duration);
        return new AsyncCreateNode<T>(() => newOperation());
    }

    public IFlow<T> ApplyTo<T>(DoOnSuccessNode<T> node) => node;
    public IFlow<T> ApplyTo<T>(AsyncDoOnSuccessNode<T> node) => node;
    public IFlow<TOut> ApplyTo<TIn, TOut>(SelectNode<TIn, TOut> node) => node;
    public IFlow<TOut> ApplyTo<TIn, TOut>(AsyncSelectNode<TIn, TOut> node) => node;

    public IFlow<TOut> ApplyTo<TIn, TOut>(ChainNode<TIn, TOut> node)
    {
        Func<TIn, Task<IFlow<TOut>>> newOperation = async (value) =>
        {
            var nextFlow = await Task.Run(() => node.Operation(value)).WaitAsync(duration);
            return ((IFlowNode<TOut>)nextFlow).Apply(this);
        };
        return new AsyncChainNode<TIn, TOut>(node.Upstream, newOperation);
    }
    public IFlow<TOut> ApplyTo<TIn, TOut>(AsyncChainNode<TIn, TOut> node)
    {
        Func<TIn, Task<IFlow<TOut>>> newOperation = async (value) =>
        {
            var nextFlow = await node.Operation(value).WaitAsync(duration);
            return ((IFlowNode<TOut>)nextFlow).Apply(this);
        };
        return node with { Operation = newOperation };
    }
}
