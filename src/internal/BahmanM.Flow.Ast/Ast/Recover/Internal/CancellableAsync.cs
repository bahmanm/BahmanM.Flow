using BahmanM.Flow.Behaviour;
using BahmanM.Flow.Shared;

namespace BahmanM.Flow.Ast.Recover.Internal;

internal sealed record CancellableAsync<T>(IFlow<T> Source, Operations.Recover.CancellableAsync<T> Recover) : INode<T>
{
    public Task<Outcome<T>> Accept(IInterpreter interpreter) => interpreter.Interpret(this);
    public IFlow<T> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
