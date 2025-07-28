namespace BahmanM.Flow.Ast;

internal interface INode<TValue> : IFlow<TValue>
{
    Task<Outcome<TValue>> Accept(Ast.IInterpreter<TValue, Task<Outcome<TValue>>> interpreter);
    IFlow<TValue> Apply(IBehaviourStrategy<TValue> strategy);
}

internal static class NodeExtensions
{
    internal static INode<TValue> AsNode<TValue>(this IFlow<TValue> flow) => flow as INode<TValue> ?? throw new InvalidOperationException();
}
