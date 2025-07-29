using BahmanM.Flow.Behaviour;

namespace BahmanM.Flow.Ast;

internal interface INode<T> : IFlow<T>
{
    Task<Outcome<T>> Accept(IInterpreter interpreter);
    IFlow<T> Apply(IBehaviourStrategy strategy);
}
