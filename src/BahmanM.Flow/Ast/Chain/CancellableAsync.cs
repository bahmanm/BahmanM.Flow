using BahmanM.Flow.Behaviour;

namespace BahmanM.Flow.Ast.Chain;

internal sealed record CancellableAsync<TIn, TOut>(
    IFlow<TIn> Upstream,
    Func<TIn, Execution.Options, Task<IFlow<TOut>>> Operation) : INode<TOut>
{
    public Task<Outcome<TOut>> Accept(IInterpreter interpreter) => interpreter.Interpret(this);
    public IFlow<TOut> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
