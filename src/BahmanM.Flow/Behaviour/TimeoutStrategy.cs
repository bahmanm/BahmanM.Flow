namespace BahmanM.Flow.Behaviour;

internal class TimeoutStrategy(TimeSpan duration) : IBehaviourStrategy
{
    public IFlow<T> ApplyTo<T>(Ast.Primitive.Succeed<T> node) => node;
    public IFlow<T> ApplyTo<T>(Ast.Primitive.Fail<T> node) => node;

    public IFlow<T> ApplyTo<T>(Ast.Create.Sync<T> node)
    {
        Flow.Operations.Create.Async<T> newOperation = () => Task.Run(() => node.Operation()).WaitAsync(duration);
        return new Ast.Create.Async<T>(newOperation);
    }

    public IFlow<T> ApplyTo<T>(Ast.Create.Async<T> node)
    {
        Flow.Operations.Create.Async<T> newOperation = () => node.Operation().WaitAsync(duration);
        return new Ast.Create.Async<T>(newOperation);
    }

    public IFlow<T> ApplyTo<T>(Ast.Create.CancellableAsync<T> node)
    {
        throw new NotImplementedException();
    }


    public IFlow<T> ApplyTo<T>(Ast.DoOnSuccess.Sync<T> node) =>
        node with { Upstream = ((Ast.INode<T>)node.Upstream).Apply(this) };

    public IFlow<T> ApplyTo<T>(Ast.DoOnSuccess.Async<T> node) =>
        node with { Upstream = ((Ast.INode<T>)node.Upstream).Apply(this) };

    public IFlow<T> ApplyTo<T>(Ast.DoOnSuccess.CancellableAsync<T> node)
    {
        throw new NotImplementedException();
    }

    public IFlow<T> ApplyTo<T>(Ast.DoOnFailure.Sync<T> node) =>
        node with { Upstream = ((Ast.INode<T>)node.Upstream).Apply(this) };

    public IFlow<T> ApplyTo<T>(Ast.DoOnFailure.Async<T> node) =>
        node with { Upstream = ((Ast.INode<T>)node.Upstream).Apply(this) };
    public IFlow<T> ApplyTo<T>(Ast.DoOnFailure.CancellableAsync<T> node) =>
        node with { Upstream = ((Ast.INode<T>)node.Upstream).Apply(this) };

    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Select.Sync<TIn, TOut> node) =>
        node with { Upstream = ((Ast.INode<TIn>)node.Upstream).Apply(this) };

    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Select.Async<TIn, TOut> node) =>
        node with { Upstream = ((Ast.INode<TIn>)node.Upstream).Apply(this) };

    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Select.CancellableAsync<TIn, TOut> node) =>
        node with { Upstream = ((Ast.INode<TIn>)node.Upstream).Apply(this) };

    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Chain.Sync<TIn, TOut> node)
    {
        Flow.Operations.Chain.Async<TIn, TOut> newOperation = async (value) =>
        {
            var nextFlow = await Task.Run(() => node.Operation(value)).WaitAsync(duration);
            return ((Ast.INode<TOut>)nextFlow).Apply(this);
        };
        return new Ast.Chain.Async<TIn, TOut>(node.Upstream, newOperation);
    }

    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Chain.Async<TIn, TOut> node)
    {
        Flow.Operations.Chain.Async<TIn, TOut> newOperation = async (value) =>
        {
            var nextFlow = await node.Operation(value).WaitAsync(duration);
            return ((Ast.INode<TOut>)nextFlow).Apply(this);
        };
        return node with { Operation = newOperation };
    }

    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Chain.CancellableAsync<TIn, TOut> node)
    {
        Flow.Operations.Chain.CancellableAsync<TIn, TOut> newOperation = async (value, cancellationToken) =>
        {
            var nextFlow = await node.Operation(value, cancellationToken).WaitAsync(duration, cancellationToken);
            return ((Ast.INode<TOut>)nextFlow).Apply(this);
        };
        return node with { Operation = newOperation };
    }

    public IFlow<T[]> ApplyTo<T>(Ast.Primitive.All<T> node)
    {
        Flow.Operations.Create.Async<T[]> newOperation = () => FlowEngine.ExecuteAsync(node).WaitAsync(duration).Unwrap();
        return new Ast.Create.Async<T[]>(newOperation);
    }

    public IFlow<T> ApplyTo<T>(Ast.Primitive.Any<T> node)
    {
        Flow.Operations.Create.Async<T> newOperation = () => FlowEngine.ExecuteAsync(node).WaitAsync(duration).Unwrap();
        return new Ast.Create.Async<T>(newOperation);
    }

}
