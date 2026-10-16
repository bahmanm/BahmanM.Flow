namespace BahmanM.Flow.Description.Select;

internal sealed record CancellableAsync<TIn, TOut>(
    IDescription<TIn> Upstream,
    Flow.Operations.Select.CancellableAsync<TIn, TOut> Operation)
    : Node<TOut>
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);
    public override Ast.INode<TOut> Lower() =>
        new Ast.Select.CancellableAsync<TIn, TOut>(Upstream.Lower(), Operation);
}
