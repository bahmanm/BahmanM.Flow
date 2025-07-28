namespace BahmanM.Flow.Ast.DoOnFailure;

internal sealed record Async<TValue>(IFlow<TValue> Upstream, Operations.DoOnFailure.Async AsyncAction) : INode<TValue>
{
    public Task<Outcome<TValue>> Accept(Ast.IInterpreter<TValue, Task<Outcome<TValue>>> interpreter) => interpreter.Interpret(this);
    public IFlow<TValue> Apply(IBehaviourStrategy<TValue> strategy) => strategy.ApplyTo(this);
}
