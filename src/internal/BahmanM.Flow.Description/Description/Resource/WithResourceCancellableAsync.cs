namespace BahmanM.Flow.Description.Resource;

internal sealed record WithResourceCancellableAsync<TResource, T>(
    Func<CancellationToken, Task<TResource>> AcquireAsync,
    Func<TResource, IFlow<T>> Use)
    : Node<T>
    where TResource : IAsyncDisposable
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);
    public override Ast.INode<T> Lower(Ast.IBehaviourStrategyFactory factory) => new Ast.Resource.WithResourceCancellableAsync<TResource, T>(AcquireAsync, Use);
}
