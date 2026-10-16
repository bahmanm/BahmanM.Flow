using BahmanM.Flow.Behaviour;

namespace BahmanM.Flow.Ast.DoOnSuccess;

internal sealed record Sync<T>(IFlow<T> Upstream, Flow.Operations.DoOnSuccess.Sync<T> Action) : INode<T>
{
    public Task<Outcome<T>> Accept(IInterpreter interpreter) => interpreter.Interpret(this);
    public IFlow<T> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
