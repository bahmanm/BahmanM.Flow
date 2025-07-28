namespace BahmanM.Flow.Ast.Create;

internal sealed record Async<TValue>(Operations.Create.Async<TValue> Operation) : INode<TValue>
{
    public Task<Outcome<TValue>> Accept(IInterpreter<TValue, Task<Outcome<TValue>>> interpreter) => interpreter.Interpret(this);
    public IFlow<TValue> Apply(IBehaviourStrategy<TValue> strategy) => strategy.ApplyTo(this);
}
