namespace BahmanM.Flow.Ast.Select;

internal sealed record Sync<TLastValue, TValue>(IFlow<TLastValue> Upstream, Operations.Select.Sync<TLastValue, TValue> Operation) : INode<TValue>
{
    public Task<Outcome<TValue>> Accept(IInterpreter<TValue, Task<Outcome<TValue>>> interpreter) => interpreter.Interpret(this);
    public IFlow<TValue> Apply(IBehaviourStrategy<TValue> strategy) => strategy.ApplyTo(this);
}
