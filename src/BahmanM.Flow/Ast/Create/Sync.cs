namespace BahmanM.Flow.Ast.Create;

internal sealed record Sync<TValue>(Operations.Create.Sync<TValue> Operation) : INode<TValue>
{
    public Task<Outcome<TValue>> Accept(Ast.IInterpreter<Task<Outcome<TValue>>> interpreter) => interpreter.Interpret(this);
    public IFlow<TValue> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
