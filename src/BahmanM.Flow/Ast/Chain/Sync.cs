namespace BahmanM.Flow.Ast.Chain;

internal sealed record Sync<TIn, TValue>(IFlow<TIn> Upstream, Operations.Chain.Sync<TIn, TValue> Operation) : INode<TValue>
{
    public Task<Outcome<TValue>> Accept(Ast.IInterpreter<TValue, Task<Outcome<TValue>>> interpreter) => interpreter.Interpret(this);
    public IFlow<TValue> Apply(IBehaviourStrategy<TValue> strategy) => strategy.ApplyTo(this);
}
