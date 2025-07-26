namespace BahmanM.Flow;

internal interface IBehaviourStrategy
{
    IFlow<T> ApplyTo<T>(SucceededNode<T> node);
    IFlow<T> ApplyTo<T>(FailedNode<T> node);
    IFlow<T> ApplyTo<T>(CreateNode<T> node);
    IFlow<T> ApplyTo<T>(AsyncCreateNode<T> node);
    IFlow<T> ApplyTo<T>(CancellableAsyncCreateNode<T> node);
    IFlow<T> ApplyTo<T>(DoOnSuccessNode<T> node);
    IFlow<T> ApplyTo<T>(AsyncDoOnSuccessNode<T> node);
    IFlow<T> ApplyTo<T>(CancellableAsyncDoOnSuccessNode<T> node);
    IFlow<T> ApplyTo<T>(DoOnFailureNode<T> node);
    IFlow<T> ApplyTo<T>(AsyncDoOnFailureNode<T> node);
    IFlow<TOut> ApplyTo<TIn, TOut>(SelectNode<TIn, TOut> node);
    IFlow<TOut> ApplyTo<TIn, TOut>(AsyncSelectNode<TIn, TOut> node);
    IFlow<TOut> ApplyTo<TIn, TOut>(CancellableAsyncSelectNode<TIn, TOut> node);
    IFlow<TOut> ApplyTo<TIn, TOut>(ChainNode<TIn, TOut> node);
    IFlow<TOut> ApplyTo<TIn, TOut>(AsyncChainNode<TIn, TOut> node);
    IFlow<TOut> ApplyTo<TIn, TOut>(CancellableAsyncChainNode<TIn, TOut> node);
    IFlow<T[]> ApplyTo<T>(AllNode<T> node);
    IFlow<T> ApplyTo<T>(AnyNode<T> node);
}
