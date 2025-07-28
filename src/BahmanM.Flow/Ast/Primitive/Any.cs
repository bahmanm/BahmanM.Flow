namespace BahmanM.Flow.Ast.Primitive;

internal sealed record Any<TValue>(IReadOnlyList<IFlow<TValue>> Flows) : INode<TValue>
{
    public Task<Outcome<TValue>> Accept(Ast.IInterpreter<Task<Outcome<TValue>>> interpreter) => interpreter.Interpret(this);
    public IFlow<TValue> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
