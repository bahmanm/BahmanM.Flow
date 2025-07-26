namespace BahmanM.Flow.Ast.Create;

internal sealed record CancellableAsync<T>(Operations.Create.CancellableAsync<T> Operation) : INode<T>
{
    public Task<Outcome<T>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
    public IFlow<T> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
