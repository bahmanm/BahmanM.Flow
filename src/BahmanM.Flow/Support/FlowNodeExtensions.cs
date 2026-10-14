namespace BahmanM.Flow.Support;

internal static class FlowNodeExtensions
{
    internal static Ast.INode<T> AsNode<T>(this IFlow<T> flow) =>
        flow is Description.IDescription<T> description
            ? description.Lower()
            : (Ast.INode<T>)flow;
}

