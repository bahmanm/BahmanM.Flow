namespace BahmanM.Flow.Ast.Pure;

internal sealed record Fail<T>(Exception Exception) : INode<T>
{
    public Task<Outcome<T>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
    public IFlow<T> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
