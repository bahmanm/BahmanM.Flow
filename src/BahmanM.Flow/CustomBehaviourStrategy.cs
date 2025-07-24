namespace BahmanM.Flow;

internal class CustomBehaviourStrategy<T>(IBehaviour<T> behaviour) : IBehaviourStrategy
{
    #region Rewriting Implementations
    
    public IFlow<TNode> ApplyTo<TNode>(CreateNode<TNode> node) =>
        node is CreateNode<T> typedNode ? (IFlow<TNode>)behaviour.Apply(typedNode) : node;

    public IFlow<TNode> ApplyTo<TNode>(AsyncCreateNode<TNode> node) =>
        node is AsyncCreateNode<T> typedNode ? (IFlow<TNode>)behaviour.Apply(typedNode) : node;

    public IFlow<TOut> ApplyTo<TIn, TOut>(ChainNode<TIn, TOut> node)
    {
        if (node is IFlow<T> flow)
        {
            return (IFlow<TOut>)behaviour.Apply(flow);
        }
        return node;
    }

    public IFlow<TOut> ApplyTo<TIn, TOut>(AsyncChainNode<TIn, TOut> node)
    {
        if (node is IFlow<T> flow)
        {
            return (IFlow<TOut>)behaviour.Apply(flow);
        }
        return node;
    }

    #endregion

    #region Pass-through Implementations
    
    public IFlow<TNode> ApplyTo<TNode>(SucceededNode<TNode> node) => node;
    public IFlow<TNode> ApplyTo<TNode>(FailedNode<TNode> node) => node;
    public IFlow<TNode> ApplyTo<TNode>(DoOnSuccessNode<TNode> node) => node;
    public IFlow<TNode> ApplyTo<TNode>(AsyncDoOnSuccessNode<TNode> node) => node;
    public IFlow<TNode> ApplyTo<TNode>(DoOnFailureNode<TNode> node) => node;
    public IFlow<TNode> ApplyTo<TNode>(AsyncDoOnFailureNode<TNode> node) => node;
    public IFlow<TOut> ApplyTo<TIn, TOut>(SelectNode<TIn, TOut> node) => node;
    public IFlow<TOut> ApplyTo<TIn, TOut>(AsyncSelectNode<TIn, TOut> node) => node;

    #endregion
}
