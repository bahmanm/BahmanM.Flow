namespace BahmanM.Flow.Ast.Chain;

internal sealed record Async<TIn, TOut>(IFlow<TIn> Upstream, Flow.Operations.Chain.Async<TIn, TOut> Operation) : INode<TOut>
{
    public Task<Outcome<TOut>> Accept(IInterpreter interpreter) => interpreter.Interpret<TIn, TOut>(this);
    public IFlow<TOut> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
