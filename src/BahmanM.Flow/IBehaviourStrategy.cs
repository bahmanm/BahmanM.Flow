namespace BahmanM.Flow;

internal interface IBehaviourStrategy
{
    IFlow<T> ApplyTo<T>(SucceededNode<T> node);
    IFlow<T> ApplyTo<T>(FailedNode<T> node);
    IFlow<T> ApplyTo<T>(CreateNode<T> node);
    IFlow<T> ApplyTo<T>(AsyncCreateNode<T> node);
    IFlow<T> ApplyTo<T>(DoOnSuccessNode<T> node);
    IFlow<T> ApplyTo<T>(AsyncDoOnSuccessNode<T> node);
    IFlow<TOut> ApplyTo<TIn, TOut>(SelectNode<TIn, TOut> node);
    IFlow<TOut> ApplyTo<TIn, TOut>(AsyncSelectNode<TIn, TOut> node);
    IFlow<TOut> ApplyTo<TIn, TOut>(ChainNode<TIn, TOut> node);
    IFlow<TOut> ApplyTo<TIn, TOut>(AsyncChainNode<TIn, TOut> node);
}
