namespace BahmanM.Flow;

#region Internal Contracts

internal interface IFlowNode<T> : IFlow<T>
{
    Task<Outcome<T>> ExecuteWith(FlowEngine engine);
    IFlow<T> Apply(IBehaviourStrategy strategy);
}

#endregion

#region Internal Flow AST Nodes

#region Source

internal sealed record SucceededFlow<T>(T Value) : IVisitableFlow<T>
{
    public Task<Outcome<T>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
    public IFlow<T> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}

internal sealed record FailedFlow<T>(Exception Exception) : IVisitableFlow<T>
{
    public Task<Outcome<T>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
    public IFlow<T> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}

#endregion

#region Create

internal sealed record CreateFlow<T>(Func<T> Operation) : IVisitableFlow<T>
{
    public Task<Outcome<T>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
    public IFlow<T> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}

internal sealed record AsyncCreateFlow<T>(Func<Task<T>> Operation) : IVisitableFlow<T>
{
    public Task<Outcome<T>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
    public IFlow<T> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}

#endregion

#region DoOnSuccess

internal sealed record DoOnSuccessFlow<T>(IFlow<T> Upstream, Action<T> Action) : IVisitableFlow<T>
{
    public Task<Outcome<T>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
    public IFlow<T> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}

internal sealed record AsyncDoOnSuccessFlow<T>(IFlow<T> Upstream, Func<T, Task> AsyncAction) : IVisitableFlow<T>
{
    public Task<Outcome<T>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
    public IFlow<T> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}

#endregion

#region DoOnFailure Nodes

internal sealed record DoOnFailureNode<T>(IFlow<T> Upstream, Action<Exception> Action) : IFlowNode<T>
{
    public Task<Outcome<T>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
    public IFlow<T> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo<T>(this);
}

internal sealed record AsyncDoOnFailureNode<T>(IFlow<T> Upstream, Func<Exception, Task> AsyncAction) : IFlowNode<T>
{
    public Task<Outcome<T>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
    public IFlow<T> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo<T>(this);
}

#endregion

#region Select Nodes

internal sealed record SelectNode<TIn, TOut>(IFlow<TIn> Upstream, Func<TIn, TOut> Operation) : IFlowNode<TOut>
{
    public Task<Outcome<TOut>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
    public IFlow<TOut> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}

internal sealed record AsyncSelectNode<TIn, TOut>(IFlow<TIn> Upstream, Func<TIn, Task<TOut>> Operation) : IFlowNode<TOut>
{
    public Task<Outcome<TOut>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
    public IFlow<TOut> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}

#endregion

#region Chain Nodes

internal sealed record ChainNode<TIn, TOut>(IFlow<TIn> Upstream, Func<TIn, IFlow<TOut>> Operation) : IFlowNode<TOut>
{
    public Task<Outcome<TOut>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
    public IFlow<TOut> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}

internal sealed record AsyncChainNode<TIn, TOut>(IFlow<TIn> Upstream, Func<TIn, Task<IFlow<TOut>>> Operation) : IFlowNode<TOut>
{
    public Task<Outcome<TOut>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
    public IFlow<TOut> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}

#endregion

#region Concurrency Nodes

internal sealed record AllNode<T>(IReadOnlyList<IFlow<T>> Flows) : IFlowNode<T[]>
{
    public Task<Outcome<T[]>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
    public IFlow<T[]> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}

internal sealed record AnyNode<T>(IReadOnlyList<IFlow<T>> Flows) : IFlowNode<T>
{
    public Task<Outcome<T>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
    public IFlow<T> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}

#endregion

#endregion