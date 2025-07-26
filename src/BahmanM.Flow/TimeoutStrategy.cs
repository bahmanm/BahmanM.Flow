namespace BahmanM.Flow;

internal class TimeoutStrategy(TimeSpan duration) : IBehaviourStrategy
{
    public IFlow<T> ApplyTo<T>(SucceededNode<T> node) => node;
    public IFlow<T> ApplyTo<T>(FailedNode<T> node) => node;

    public IFlow<T> ApplyTo<T>(CreateNode<T> node)
    {
        Operations.Create.Async<T> newOperation = () => Task.Run(() => node.Operation()).WaitAsync(duration);
        return new AsyncCreateNode<T>(newOperation);
    }

    public IFlow<T> ApplyTo<T>(AsyncCreateNode<T> node)
    {
        Operations.Create.Async<T> newOperation = () => node.Operation().WaitAsync(duration);
        return new AsyncCreateNode<T>(newOperation);
    }

    public IFlow<T> ApplyTo<T>(CancellableAsyncCreateNode<T> node)
    {
        throw new NotImplementedException();
    }


    public IFlow<T> ApplyTo<T>(DoOnSuccessNode<T> node) =>
        node with { Upstream = ((IFlowNode<T>)node.Upstream).Apply(this) };

    public IFlow<T> ApplyTo<T>(AsyncDoOnSuccessNode<T> node) =>
        node with { Upstream = ((IFlowNode<T>)node.Upstream).Apply(this) };

    public IFlow<T> ApplyTo<T>(CancellableAsyncDoOnSuccessNode<T> node)
    {
        throw new NotImplementedException();
    }

    public IFlow<T> ApplyTo<T>(DoOnFailureNode<T> node) =>
        node with { Upstream = ((IFlowNode<T>)node.Upstream).Apply(this) };

    public IFlow<T> ApplyTo<T>(AsyncDoOnFailureNode<T> node) =>
        node with { Upstream = ((IFlowNode<T>)node.Upstream).Apply(this) };
    public IFlow<T> ApplyTo<T>(CancellableAsyncDoOnFailureNode<T> node) =>
        node with { Upstream = ((IFlowNode<T>)node.Upstream).Apply(this) };

    public IFlow<TOut> ApplyTo<TIn, TOut>(SelectNode<TIn, TOut> node) =>
        node with { Upstream = ((IFlowNode<TIn>)node.Upstream).Apply(this) };

    public IFlow<TOut> ApplyTo<TIn, TOut>(AsyncSelectNode<TIn, TOut> node) =>
        node with { Upstream = ((IFlowNode<TIn>)node.Upstream).Apply(this) };

    public IFlow<TOut> ApplyTo<TIn, TOut>(CancellableAsyncSelectNode<TIn, TOut> node) =>
        node with { Upstream = ((IFlowNode<TIn>)node.Upstream).Apply(this) };

    public IFlow<TOut> ApplyTo<TIn, TOut>(ChainNode<TIn, TOut> node)
    {
        Operations.Chain.Async<TIn, TOut> newOperation = async (value) =>
        {
            var nextFlow = await Task.Run(() => node.Operation(value)).WaitAsync(duration);
            return ((IFlowNode<TOut>)nextFlow).Apply(this);
        };
        return new AsyncChainNode<TIn, TOut>(node.Upstream, newOperation);
    }

    public IFlow<TOut> ApplyTo<TIn, TOut>(AsyncChainNode<TIn, TOut> node)
    {
        Operations.Chain.Async<TIn, TOut> newOperation = async (value) =>
        {
            var nextFlow = await node.Operation(value).WaitAsync(duration);
            return ((IFlowNode<TOut>)nextFlow).Apply(this);
        };
        return node with { Operation = newOperation };
    }

    public IFlow<TOut> ApplyTo<TIn, TOut>(CancellableAsyncChainNode<TIn, TOut> node)
    {
        Operations.Chain.CancellableAsync<TIn, TOut> newOperation = async (value, cancellationToken) =>
        {
            var nextFlow = await node.Operation(value, cancellationToken).WaitAsync(duration, cancellationToken);
            return ((IFlowNode<TOut>)nextFlow).Apply(this);
        };
        return node with { Operation = newOperation };
    }

    public IFlow<T[]> ApplyTo<T>(AllNode<T> node)
    {
        Operations.Create.Async<T[]> newOperation = () => FlowEngine.ExecuteAsync(node).WaitAsync(duration).Unwrap();
        return new AsyncCreateNode<T[]>(newOperation);
    }

    public IFlow<T> ApplyTo<T>(AnyNode<T> node)
    {
        Operations.Create.Async<T> newOperation = () => FlowEngine.ExecuteAsync(node).WaitAsync(duration).Unwrap();
        return new AsyncCreateNode<T>(newOperation);
    }

}
