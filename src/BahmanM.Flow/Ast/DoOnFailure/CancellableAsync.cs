namespace BahmanM.Flow.Ast.DoOnFailure;

internal sealed record CancellableAsync<T>(
    IFlow<T> Upstream,
    Operations.DoOnFailure.CancellableAsync AsyncAction) : INode<T>
{
    public Task<Outcome<T>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
    public IFlow<T> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
