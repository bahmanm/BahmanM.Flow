namespace BahmanM.Flow.Support;

internal static class FlowNodeExtensions
{
    extension<T>(IFlow<T> flow)
    {
        internal Description.IDescription<T> AsDescription() =>
            flow as Description.IDescription<T> ?? new Description.ExistingFlow<T>(flow);

        internal Ast.INode<T> AsNode(Ast.IBehaviourStrategyFactory factory) =>
            flow is Description.IDescription<T> description
                ? description.Lower(factory)
                : (Ast.INode<T>)flow;
    }
}

