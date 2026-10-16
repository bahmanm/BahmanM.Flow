namespace BahmanM.Flow.Description.Resource;

internal sealed record WithResource<TResource, T>(
    Func<TResource> Acquire,
    Func<TResource, IFlow<T>> Use)
    : Node<T>
    where TResource : IDisposable
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);
    public override Ast.INode<T> Lower() => new Ast.Resource.WithResource<TResource, T>(Acquire, Use);
}
