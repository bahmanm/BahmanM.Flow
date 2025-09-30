using BahmanM.Flow.Support;

using BahmanM.Flow.Behaviour.Support;

namespace BahmanM.Flow.Behaviour;

internal class TimeoutStrategy(TimeSpan duration) : IBehaviourStrategy
{
    public IFlow<T> ApplyTo<T>(Ast.Primitive.Succeed<T> node) => node;
    public IFlow<T> ApplyTo<T>(Ast.Primitive.Fail<T> node) => node;

    public IFlow<T> ApplyTo<T>(Ast.Create.Sync<T> node)
    {
        Func<CancellationToken, Task<T>> newOperation = parentScopeToken =>
            TimedScope.ExecuteAsync(
                duration,
                parentScopeToken,
                _ => Task.Run(() => node.Operation()));
        return new Ast.Create.CancellableAsync<T>(newOperation);
    }

    public IFlow<T> ApplyTo<T>(Ast.Create.Async<T> node)
    {
        Func<CancellationToken, Task<T>> newOperation = parentScopeToken =>
            TimedScope.ExecuteAsync(
                duration,
                parentScopeToken,
                _ => node.Operation());
        return new Ast.Create.CancellableAsync<T>(newOperation);
    }

    public IFlow<T> ApplyTo<T>(Ast.Create.CancellableAsync<T> node)
    {
        Func<CancellationToken, Task<T>> newOperation = parentScopeToken =>
            TimedScope.ExecuteAsync(
                duration,
                parentScopeToken,
                childScopeToken => node.Operation(childScopeToken));
        return new Ast.Create.CancellableAsync<T>(newOperation);
    }


    public IFlow<T> ApplyTo<T>(Ast.DoOnSuccess.Sync<T> node) =>
        node with { Upstream = node.Upstream.AsNode().Apply(this) };

    public IFlow<T> ApplyTo<T>(Ast.DoOnSuccess.Async<T> node) =>
        node with { Upstream = node.Upstream.AsNode().Apply(this) };

    public IFlow<T> ApplyTo<T>(Ast.DoOnSuccess.CancellableAsync<T> node) =>
        node with { Upstream = node.Upstream.AsNode().Apply(this) };

    public IFlow<T> ApplyTo<T>(Ast.DoOnFailure.Sync<T> node) =>
        node with { Upstream = node.Upstream.AsNode().Apply(this) };

    public IFlow<T> ApplyTo<T>(Ast.DoOnFailure.Async<T> node) =>
        node with { Upstream = node.Upstream.AsNode().Apply(this) };
    public IFlow<T> ApplyTo<T>(Ast.DoOnFailure.CancellableAsync<T> node) =>
        node with { Upstream = node.Upstream.AsNode().Apply(this) };

    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Select.Sync<TIn, TOut> node) =>
        node with { Upstream = node.Upstream.AsNode().Apply(this) };

    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Select.Async<TIn, TOut> node) =>
        node with { Upstream = node.Upstream.AsNode().Apply(this) };

    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Select.CancellableAsync<TIn, TOut> node) =>
        node with { Upstream = node.Upstream.AsNode().Apply(this) };

    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Chain.Sync<TIn, TOut> node)
    {
        Flow.Operations.Chain.CancellableAsync<TIn, TOut> newOperation = async (value, parentScopeToken) =>
        {
            var nextFlow = await TimedScope.ExecuteAsync(
                duration,
                parentScopeToken,
                _ => Task.Run(() => node.Operation(value)));
            return nextFlow.AsNode().Apply(this);
        };
        return new Ast.Chain.CancellableAsync<TIn, TOut>(node.Upstream, newOperation);
    }

    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Chain.Async<TIn, TOut> node)
    {
        Flow.Operations.Chain.CancellableAsync<TIn, TOut> newOperation = async (value, parentScopeToken) =>
        {
            var nextFlow = await TimedScope.ExecuteAsync(
                duration,
                parentScopeToken,
                _ => node.Operation(value));
            return nextFlow.AsNode().Apply(this);
        };
        return new Ast.Chain.CancellableAsync<TIn, TOut>(node.Upstream, newOperation);
    }

    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Chain.CancellableAsync<TIn, TOut> node)
    {
        Flow.Operations.Chain.CancellableAsync<TIn, TOut> newOperation = async (value, parentScopeToken) =>
        {
            var nextFlow = await TimedScope.ExecuteAsync(
                duration,
                parentScopeToken,
                childScopeToken => node.Operation(value, childScopeToken));
            return nextFlow.AsNode().Apply(this);
        };
        return node with { Operation = newOperation };
    }

    public IFlow<T> ApplyTo<T>(Ast.Recover.Sync<T> node) =>
        node with { Source = node.Source.AsNode().Apply(this) };

    public IFlow<T> ApplyTo<T>(Ast.Recover.Async<T> node) =>
        node with { Source = node.Source.AsNode().Apply(this) };

    public IFlow<T> ApplyTo<T>(Ast.Recover.CancellableAsync<T> node) =>
        node with { Source = node.Source.AsNode().Apply(this) };

    public IFlow<T[]> ApplyTo<T>(Ast.Primitive.All<T> node)
    {
        Func<CancellationToken, Task<T[]>> newOperation = parentScopeToken =>
            TimedScope.ExecuteAsync(
                duration,
                parentScopeToken,
                childScopeToken => FlowEngine
                    .ExecuteAsync(node, new Execution.Options(childScopeToken))
                    .Unwrap());
        return new Ast.Create.CancellableAsync<T[]>(newOperation);
    }

    public IFlow<T> ApplyTo<T>(Ast.Primitive.Any<T> node)
    {
        Func<CancellationToken, Task<T>> newOperation = parentScopeToken =>
            TimedScope.ExecuteAsync(
                duration,
                parentScopeToken,
                childScopeToken => FlowEngine
                    .ExecuteAsync(node, new Execution.Options(childScopeToken))
                    .Unwrap());
        return new Ast.Create.CancellableAsync<T>(newOperation);
    }

    public IFlow<T> ApplyTo<T>(Ast.Validate.Sync<T> node) =>
        node with { Upstream = node.Upstream.AsNode().Apply(this) };

    public IFlow<T> ApplyTo<T>(Ast.Validate.Async<T> node) =>
        node with { Upstream = node.Upstream.AsNode().Apply(this) };

    public IFlow<T> ApplyTo<T>(Ast.Validate.CancellableAsync<T> node) =>
        node with { Upstream = node.Upstream.AsNode().Apply(this) };

    public IFlow<T> ApplyTo<TResource, T>(Ast.Resource.WithResource<TResource, T> node) where TResource : IDisposable
    {
        Func<CancellationToken, Task<T>> newOperation = parentScopeToken =>
            TimedScope.ExecuteAsync(
                duration,
                parentScopeToken,
                childScopeToken => FlowEngine
                    .ExecuteAsync(node, new Execution.Options(childScopeToken))
                    .Unwrap());
        return new Ast.Create.CancellableAsync<T>(newOperation);
    }

    public IFlow<T> ApplyTo<TResource, T>(Ast.Resource.WithResourceAsync<TResource, T> node) where TResource : IAsyncDisposable
    {
        Func<CancellationToken, Task<T>> newOperation = parentScopeToken =>
            TimedScope.ExecuteAsync(
                duration,
                parentScopeToken,
                childScopeToken => FlowEngine
                    .ExecuteAsync(node, new Execution.Options(childScopeToken))
                    .Unwrap());
        return new Ast.Create.CancellableAsync<T>(newOperation);
    }

    public IFlow<T> ApplyTo<TResource, T>(Ast.Resource.WithResourceCancellableAsync<TResource, T> node) where TResource : IAsyncDisposable
    {
        Func<CancellationToken, Task<T>> newOperation = parentScopeToken =>
            TimedScope.ExecuteAsync(
                duration,
                parentScopeToken,
                childScopeToken => FlowEngine
                    .ExecuteAsync(node, new Execution.Options(childScopeToken))
                    .Unwrap());
        return new Ast.Create.CancellableAsync<T>(newOperation);
    }
}
