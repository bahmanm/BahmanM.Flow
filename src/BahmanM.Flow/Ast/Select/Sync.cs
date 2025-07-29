namespace BahmanM.Flow.Ast.Select;

internal sealed record Sync<TIn, TOut>(IFlow<TIn> Upstream, Operations.Select.Sync<TIn, TOut> Operation) : INode<TOut>
{
    public Task<Outcome<TOut>> Accept(IInterpreter interpreter) => interpreter.Interpret(this);
    public IFlow<TOut> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
