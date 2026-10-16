namespace BahmanM.Flow.Description.Validate;

internal sealed record Async<T>(
    IDescription<T> Upstream,
    Func<T, Task<bool>> PredicateAsync,
    Func<T, Exception> ExceptionFactory)
    : Node<T>
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);

    public override Ast.INode<T> Lower() =>
        new Ast.Validate.Async<T>(Upstream.Lower(), PredicateAsync, ExceptionFactory);
}
