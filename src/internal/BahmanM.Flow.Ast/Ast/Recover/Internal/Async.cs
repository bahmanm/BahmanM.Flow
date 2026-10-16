using BahmanM.Flow.Behaviour;
using BahmanM.Flow.Shared;

namespace BahmanM.Flow.Ast.Recover.Internal;

internal sealed record Async<T>(IFlow<T> Source, Operations.Recover.Async<T> Recover) : INode<T>
{
    public Task<Outcome<T>> Accept(IInterpreter interpreter) => interpreter.Interpret(this);
    public IFlow<T> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
