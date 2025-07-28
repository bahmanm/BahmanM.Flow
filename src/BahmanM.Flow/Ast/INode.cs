namespace BahmanM.Flow.Ast;

internal interface INode<TValue> : IFlow<TValue>
{
    Task<Outcome<TValue>> Accept(Ast.IInterpreter<Task<Outcome<TValue>>> interpreter);
    IFlow<TValue> Apply(IBehaviourStrategy strategy);
}
