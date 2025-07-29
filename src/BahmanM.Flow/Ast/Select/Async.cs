namespace BahmanM.Flow.Ast.Select;

internal sealed record Async<TIn, TOut>(IFlow<TIn> Upstream, Flow.Operations.Select.Async<TIn, TOut> Operation) : INode<TOut>
{
    public Task<Outcome<TOut>> Accept(IInterpreter interpreter) => interpreter.Interpret(this);
    public IFlow<TOut> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
