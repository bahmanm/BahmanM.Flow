namespace BahmanM.Flow.Ast.DoOnSuccess;

internal sealed record Sync<T>(IFlow<T> Upstream, Operations.DoOnSuccess.Sync<T> Action) : INode<T>
{
    public Task<Outcome<T>> Accept(IInterpreter<T> interpreter) => interpreter.Interpret(this);
    public IFlow<T> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
