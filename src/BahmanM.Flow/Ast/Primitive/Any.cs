namespace BahmanM.Flow.Ast.Primitive;

internal sealed record Any<TValue>(IReadOnlyList<IFlow<TValue>> Flows) : INode<TValue>
{
    public Task<Outcome<TValue>> Accept(Ast.IInterpreter<TValue, Task<Outcome<TValue>>> interpreter) => interpreter.Interpret(this);
    public IFlow<TValue> Apply(IBehaviourStrategy<TValue> strategy) => strategy.ApplyTo(this);
}
