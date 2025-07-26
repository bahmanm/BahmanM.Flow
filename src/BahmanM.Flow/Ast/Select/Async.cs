namespace BahmanM.Flow.Ast.Select;

internal sealed record Async<TIn, TOut>(IFlow<TIn> Upstream, Operations.Select.Async<TIn, TOut> Operation) : INode<TOut>
{
    public Task<Outcome<TOut>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
    public IFlow<TOut> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
