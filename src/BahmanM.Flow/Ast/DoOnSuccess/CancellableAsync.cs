namespace BahmanM.Flow.Ast.DoOnSuccess;

internal sealed record CancellableAsync<TValue>(IFlow<TValue> Upstream, Operations.DoOnSuccess.CancellableAsync<TValue> AsyncAction) : INode<TValue>
{
    public Task<Outcome<TValue>> Accept(IInterpreter<Task<Outcome<TValue>>> interpreter) => interpreter.Interpret(this);
    public IFlow<TValue> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
