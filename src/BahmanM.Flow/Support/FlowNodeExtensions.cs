namespace BahmanM.Flow.Support;

internal static class FlowNodeExtensions
{
    internal static Ast.INode<T> AsNode<T>(this IFlow<T> flow) => (Ast.INode<T>)flow;
}

