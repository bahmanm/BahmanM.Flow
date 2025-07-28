namespace BahmanM.Flow.Ast.DoOnSuccess;

internal sealed record Sync<TValue>(IFlow<TValue> Upstream, Operations.DoOnSuccess.Sync<TValue> Action) : INode<TValue>
{
    public Task<Outcome<TValue>> Accept(Ast.IInterpreter<Task<Outcome<TValue>>> interpreter) => interpreter.Interpret(this);
    public IFlow<TValue> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
