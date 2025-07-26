namespace BahmanM.Flow.Ast.DoOnFailure;

internal sealed record Sync<T>(IFlow<T> Upstream, Operations.DoOnFailure.Sync Action) : INode<T>
{
    public Task<Outcome<T>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
    public IFlow<T> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
