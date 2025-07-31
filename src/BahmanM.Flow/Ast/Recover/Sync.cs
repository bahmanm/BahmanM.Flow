using BahmanM.Flow.Behaviour;

namespace BahmanM.Flow.Ast.Recover;

public sealed record Sync<T> (IFlow<T> Source, Flow.Operations.Recover.Sync<T> Recover) : INode<T>
{
    Task<Outcome<T>> INode<T>.Accept(IInterpreter interpreter) => interpreter.Interpret(this);

    IFlow<T> INode<T>.Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
