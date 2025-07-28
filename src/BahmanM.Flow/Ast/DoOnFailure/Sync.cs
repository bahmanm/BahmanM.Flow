namespace BahmanM.Flow.Ast.DoOnFailure;

internal sealed record Sync<TValue>(IFlow<TValue> Upstream, Operations.DoOnFailure.Sync Action) : INode<TValue>
{
    public Task<Outcome<TValue>> Accept(Ast.IInterpreter<TValue, Task<Outcome<TValue>>> interpreter) => interpreter.Interpret(this);
    public IFlow<TValue> Apply(IBehaviourStrategy<TValue> strategy) => strategy.ApplyTo(this);
}
