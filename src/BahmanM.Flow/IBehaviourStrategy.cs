namespace BahmanM.Flow;

// The 'Visitor' interface for applying behaviours.
// It defines a method for each 'Element' (failable node) type that it supports.
internal interface IBehaviourStrategy
{
    IFlow<T> ApplyTo<T>(CreateNode<T> node);
    IFlow<T> ApplyTo<TIn, T>(ChainNode<TIn, T> node);
    IFlow<T> ApplyTo<T>(AsyncCreateNode<T> node);

    // TODO: Add overloads for async nodes

    // Default for nodes that are not explicitly handled by a strategy.
    // This ensures that if a new node type is added, we get a loud failure
    // instead of silently ignoring the behaviour.
    IFlow<T> ApplyTo<T>(IFlowNode<T> node) =>
        throw new NotSupportedException($"The behaviour strategy does not support the node type {node.GetType().Name}.");
}