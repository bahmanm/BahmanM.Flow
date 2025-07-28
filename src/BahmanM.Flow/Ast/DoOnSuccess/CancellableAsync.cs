namespace BahmanM.Flow.Ast.DoOnSuccess;

internal sealed record CancellableAsync<TValue>(IFlow<TValue> Upstream, Operations.DoOnSuccess.CancellableAsync<TValue> AsyncAction) : INode<TValue>
{
    public Task<Outcome<TValue>> Accept(IInterpreter<TValue, Task<Outcome<TValue>>> interpreter) => interpreter.Interpret(this);
    public IFlow<TValue> Apply(IBehaviourStrategy<TValue> strategy) => strategy.ApplyTo(this);
}
