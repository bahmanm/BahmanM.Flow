namespace BahmanM.Flow.Ast.DoOnSuccess;

internal sealed record Sync<T>(IFlow<T> Upstream, Operations.DoOnSuccess.Sync<T> Action) : INode<T>
{
    public Task<Outcome<T>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
    public IFlow<T> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
