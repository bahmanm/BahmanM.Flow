namespace BahmanM.Flow.Ast.Primitive;

internal sealed record All<T>(IReadOnlyList<IFlow<T>> Flows) : INode<T[]>
{
    public Task<Outcome<T[]>> Accept(IInterpreter interpreter) => interpreter.Interpret(this);
    public IFlow<T[]> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
