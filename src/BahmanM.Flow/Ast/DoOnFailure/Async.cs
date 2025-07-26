namespace BahmanM.Flow.Ast.DoOnFailure;

internal sealed record Async<T>(IFlow<T> Upstream, Operations.DoOnFailure.Async AsyncAction) : INode<T>
{
    public Task<Outcome<T>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
    public IFlow<T> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
