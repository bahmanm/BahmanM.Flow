namespace BahmanM.Flow.Ast.Create;

internal sealed record CancellableAsync<TValue>(Func<CancellationToken, Task<TValue>> Operation) : INode<TValue>
{
    public Task<Outcome<TValue>> Accept(Ast.IInterpreter<TValue, Task<Outcome<TValue>>> interpreter) => interpreter.Interpret(this);
    public IFlow<TValue> Apply(IBehaviourStrategy<TValue> strategy) => strategy.ApplyTo(this);
}
