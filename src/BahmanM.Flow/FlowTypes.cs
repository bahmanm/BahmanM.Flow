namespace BahmanM.Flow;

#region Internal Flow AST Nodes

#region Source

internal sealed record SucceededFlow<T>(T Value) : IVisitableFlow<T>
{
    public Task<Outcome<T>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
}

internal sealed record FailedFlow<T>(Exception Exception) : IVisitableFlow<T>
{
    public Task<Outcome<T>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
}

#endregion

#region Create

internal sealed record CreateFlow<T>(Func<T> Operation) : IVisitableFlow<T>
{
    public Task<Outcome<T>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
}

internal sealed record AsyncCreateFlow<T>(Func<Task<T>> Operation) : IVisitableFlow<T>
{
    public Task<Outcome<T>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
}

#endregion

#region DoOnSuccess

internal sealed record DoOnSuccessFlow<T>(IFlow<T> Upstream, Action<T> Action) : IVisitableFlow<T>
{
    public Task<Outcome<T>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
}

internal sealed record AsyncDoOnSuccessFlow<T>(IFlow<T> Upstream, Func<T, Task> AsyncAction) : IVisitableFlow<T>
{
    public Task<Outcome<T>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
}

#endregion

#region Select

internal sealed record SelectFlow<TIn, TOut>(IFlow<TIn> Upstream, Func<TIn, TOut> Operation) : IVisitableFlow<TOut>
{
    public Task<Outcome<TOut>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
}

internal sealed record AsyncSelectFlow<TIn, TOut>(IFlow<TIn> Upstream, Func<TIn, Task<TOut>> Operation) : IVisitableFlow<TOut>
{
    public Task<Outcome<TOut>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
}

#endregion

#endregion
