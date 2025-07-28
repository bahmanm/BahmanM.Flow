namespace BahmanM.Flow.Ast.Create;

internal sealed record CancellableAsync<TValue>(Func<CancellationToken, Task<TValue>> Operation) : INode<TValue>
{
    public Task<Outcome<TValue>> Accept(Ast.IInterpreter<Task<Outcome<TValue>>> interpreter) => interpreter.Interpret(this);
    public IFlow<TValue> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
