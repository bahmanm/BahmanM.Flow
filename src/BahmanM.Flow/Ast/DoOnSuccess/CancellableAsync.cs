namespace BahmanM.Flow.Ast.DoOnSuccess;

internal sealed record CancellableAsync<T>(
    IFlow<T> Upstream,
    Operations.DoOnSuccess.CancellableAsync<T> AsyncAction) : INode<T>
{
    public Task<Outcome<T>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
    public IFlow<T> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
