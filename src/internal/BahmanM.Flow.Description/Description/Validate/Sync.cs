namespace BahmanM.Flow.Description.Validate;

internal sealed record Sync<T>(
    IDescription<T> Upstream,
    Func<T, bool> Predicate,
    Func<T, Exception> ExceptionFactory)
    : Node<T>
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);
    public override Ast.INode<T> Lower(Ast.IBehaviourStrategyFactory factory) =>
        new Ast.Validate.Sync<T>(Upstream.Lower(factory), Predicate, ExceptionFactory);
}
