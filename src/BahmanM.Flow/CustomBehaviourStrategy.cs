namespace BahmanM.Flow;

internal class CustomBehaviourStrategy(IBehaviour behaviour) : IBehaviourStrategy
{
    public IFlow<T> ApplyTo<T>(SucceededNode<T> node) => behaviour.Apply(node);
    public IFlow<T> ApplyTo<T>(FailedNode<T> node) => behaviour.Apply(node);
    public IFlow<T> ApplyTo<T>(CreateNode<T> node) => behaviour.Apply(node);
    public IFlow<T> ApplyTo<T>(AsyncCreateNode<T> node) => behaviour.Apply(node);
    public IFlow<T> ApplyTo<T>(CancellableAsyncCreateNode<T> node) => behaviour.Apply(node);
    public IFlow<T> ApplyTo<T>(DoOnSuccessNode<T> node) => behaviour.Apply(node);
    public IFlow<T> ApplyTo<T>(AsyncDoOnSuccessNode<T> node) => behaviour.Apply(node);
    public IFlow<T> ApplyTo<T>(CancellableAsyncDoOnSuccessNode<T> node) => behaviour.Apply(node);
    public IFlow<T> ApplyTo<T>(DoOnFailureNode<T> node) => behaviour.Apply(node);
    public IFlow<T> ApplyTo<T>(AsyncDoOnFailureNode<T> node) => behaviour.Apply(node);
    public IFlow<T> ApplyTo<T>(CancellableAsyncDoOnFailureNode<T> node) => behaviour.Apply(node);
    public IFlow<TOut> ApplyTo<TIn, TOut>(SelectNode<TIn, TOut> node) => behaviour.Apply(node);
    public IFlow<TOut> ApplyTo<TIn, TOut>(AsyncSelectNode<TIn, TOut> node) => behaviour.Apply(node);
    public IFlow<TOut> ApplyTo<TIn, TOut>(CancellableAsyncSelectNode<TIn, TOut> node) => behaviour.Apply(node);
    public IFlow<TOut> ApplyTo<TIn, TOut>(ChainNode<TIn, TOut> node) => behaviour.Apply(node);
    public IFlow<TOut> ApplyTo<TIn, TOut>(AsyncChainNode<TIn, TOut> node) => behaviour.Apply(node);
    public IFlow<TOut> ApplyTo<TIn, TOut>(CancellableAsyncChainNode<TIn, TOut> node) => behaviour.Apply(node);
    public IFlow<T[]> ApplyTo<T>(AllNode<T> node) => behaviour.Apply(node);
    public IFlow<T> ApplyTo<T>(AnyNode<T> node) => behaviour.Apply(node);
}
