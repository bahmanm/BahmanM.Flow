namespace BahmanM.Flow.Ast.Chain;

internal sealed record Sync<TIn, TOut>(IFlow<TIn> Upstream, Operations.Chain.Sync<TIn, TOut> Operation) : INode<TOut>
{
    public Task<Outcome<TOut>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
    public IFlow<TOut> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
