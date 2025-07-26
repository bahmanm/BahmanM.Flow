namespace BahmanM.Flow.Ast;

internal interface INode<T> : IFlow<T>
{
    Task<Outcome<T>> ExecuteWith(FlowEngine engine);
    IFlow<T> Apply(IBehaviourStrategy strategy);
}
