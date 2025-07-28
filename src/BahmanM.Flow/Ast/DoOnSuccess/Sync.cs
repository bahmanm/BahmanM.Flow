namespace BahmanM.Flow.Ast.DoOnSuccess;

internal sealed record Sync<TValue>(IFlow<TValue> Upstream, Operations.DoOnSuccess.Sync<TValue> Action) : INode<TValue>
{
    public Task<Outcome<TValue>> Accept(Ast.IInterpreter<TValue, Task<Outcome<TValue>>> interpreter) => interpreter.Interpret(this);
    public IFlow<TValue> Apply(IBehaviourStrategy<TValue> strategy) => strategy.ApplyTo(this);
}
