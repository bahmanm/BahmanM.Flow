namespace BahmanM.Flow.Ast.DoOnFailure;

internal sealed record CancellableAsync<TValue>(
    IFlow<TValue> Upstream,
    Operations.DoOnFailure.CancellableAsync AsyncAction) : INode<TValue>
{
    public Task<Outcome<TValue>> Accept(Ast.IInterpreter<Task<Outcome<TValue>>> interpreter) => interpreter.Interpret(this);
    public IFlow<TValue> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
