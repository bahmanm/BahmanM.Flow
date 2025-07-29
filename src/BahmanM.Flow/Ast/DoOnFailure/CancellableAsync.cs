using BahmanM.Flow.Behaviour;

namespace BahmanM.Flow.Ast.DoOnFailure;

internal sealed record CancellableAsync<T>(
    IFlow<T> Upstream,
    Flow.Operations.DoOnFailure.CancellableAsync AsyncAction) : INode<T>
{
    public Task<Outcome<T>> Accept(IInterpreter interpreter) => interpreter.Interpret(this);
    public IFlow<T> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
