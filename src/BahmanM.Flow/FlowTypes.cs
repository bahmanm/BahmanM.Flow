namespace BahmanM.Flow;

#region Internal Contracts

internal interface IFlowNode<T> : IFlow<T>
{
    Task<Outcome<T>> ExecuteWith(FlowEngine engine);
}

#endregion

#region Internal Flow AST Nodes

#region Source Nodes

internal sealed record SucceededNode<T>(T Value) : IFlowNode<T>
{
    public Task<Outcome<T>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
}

internal sealed record FailedNode<T>(Exception Exception) : IFlowNode<T>
{
    public Task<Outcome<T>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
}

#endregion

#region Create Nodes

internal sealed record CreateNode<T>(Func<T> Operation) : IFlowNode<T>
{
    public Task<Outcome<T>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
}

internal sealed record AsyncCreateNode<T>(Func<Task<T>> Operation) : IFlowNode<T>
{
    public Task<Outcome<T>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
}

#endregion

#region DoOnSuccess Nodes

internal sealed record DoOnSuccessNode<T>(IFlow<T> Upstream, Action<T> Action) : IFlowNode<T>
{
    public Task<Outcome<T>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
}

internal sealed record AsyncDoOnSuccessNode<T>(IFlow<T> Upstream, Func<T, Task> AsyncAction) : IFlowNode<T>
{
    public Task<Outcome<T>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
}

#endregion

#region Select Nodes

internal sealed record SelectNode<TIn, TOut>(IFlow<TIn> Upstream, Func<TIn, TOut> Operation) : IFlowNode<TOut>
{
    public Task<Outcome<TOut>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
}

internal sealed record AsyncSelectNode<TIn, TOut>(IFlow<TIn> Upstream, Func<TIn, Task<TOut>> Operation) : IFlowNode<TOut>
{
    public Task<Outcome<TOut>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
}

#endregion

#endregion