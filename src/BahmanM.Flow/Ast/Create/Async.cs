namespace BahmanM.Flow.Ast.Create;

internal sealed record Async<TValue>(Operations.Create.Async<TValue> Operation) : INode<TValue>
{
    public Task<Outcome<TValue>> Accept(IInterpreter<Task<Outcome<TValue>>> interpreter) => interpreter.Interpret(this);
    public IFlow<TValue> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
