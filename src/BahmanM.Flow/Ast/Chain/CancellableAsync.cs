namespace BahmanM.Flow.Ast.Chain;

internal sealed record CancellableAsync<TIn, TOut>(
    IFlow<TIn> Upstream,
    Operations.Chain.CancellableAsync<TIn, TOut> Operation) : INode<TOut>
{
    public Task<Outcome<TOut>> Accept(IInterpreter<TOut> interpreter) => interpreter.Interpret(this);
    public IFlow<TOut> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
