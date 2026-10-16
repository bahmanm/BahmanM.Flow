namespace BahmanM.Flow.Description.Resource;

internal sealed record WithResourceAsync<TResource, T>(
    Func<Task<TResource>> AcquireAsync,
    Func<TResource, IFlow<T>> Use)
    : Node<T>
    where TResource : IAsyncDisposable
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);
    public override Ast.INode<T> Lower(Ast.IBehaviourStrategyFactory factory) => new Ast.Resource.WithResourceAsync<TResource, T>(AcquireAsync, Use);
}
