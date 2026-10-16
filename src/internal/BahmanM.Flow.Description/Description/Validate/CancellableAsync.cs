namespace BahmanM.Flow.Description.Validate;

internal sealed record CancellableAsync<T>(
    IDescription<T> Upstream,
    Func<T, CancellationToken, Task<bool>> PredicateCancellableAsync,
    Func<T, Exception> ExceptionFactory)
    : Node<T>
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);

    public override Ast.INode<T> Lower(Ast.IBehaviourStrategyFactory factory) =>
        new Ast.Validate.CancellableAsync<T>(Upstream.Lower(factory), PredicateCancellableAsync, ExceptionFactory);
}
