namespace BahmanM.Flow.Ast.Chain;

internal sealed record CancellableAsync<TIn, TValue>(
    IFlow<TIn> Upstream,
    Operations.Chain.CancellableAsync<TIn, TValue> Operation) : INode<TValue>
{
    public Task<Outcome<TValue>> Accept(Ast.IInterpreter<Task<Outcome<TValue>>> interpreter) => interpreter.Interpret(this);
    public IFlow<TValue> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
