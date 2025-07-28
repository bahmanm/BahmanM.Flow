namespace BahmanM.Flow.Ast.Primitive;

internal sealed record All<TValue>(IReadOnlyList<IFlow<TValue>> Flows)
{
    public Task<Outcome<IList<TValue>>> Accept(Ast.IInterpreter<TValue, Task<Outcome<IList<TValue>>>> interpreter) => interpreter.Interpret(this);
    public IFlow<IList<TValue>> Apply(IBehaviourStrategy<TValue> strategy) => strategy.ApplyTo(this);
}
