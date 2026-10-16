namespace BahmanM.Flow.Support;

internal static class FlowNodeExtensions
{
    internal static Description.IDescription<T> AsDescription<T>(this IFlow<T> flow) =>
        flow as Description.IDescription<T> ?? new Description.ExistingFlow<T>(flow);

    internal static Ast.INode<T> AsNode<T>(this IFlow<T> flow) =>
        flow is Description.IDescription<T> description
            ? description.Lower()
            : (Ast.INode<T>)flow;
}

