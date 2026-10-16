using BahmanM.Flow.Behaviour;

namespace BahmanM.Flow.Ast.Create;

internal sealed record Sync<T>(BahmanM.Flow.Shared.Operations.Create.Sync<T> Operation) : INode<T>
{
    public Task<Outcome<T>> Accept(IInterpreter interpreter) => interpreter.Interpret(this);
    public IFlow<T> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
