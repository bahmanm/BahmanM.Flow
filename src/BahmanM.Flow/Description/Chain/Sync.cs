namespace BahmanM.Flow.Description.Chain;

internal sealed record Sync<TIn, TOut>(
    IDescription<TIn> Upstream,
    Flow.Operations.Chain.Sync<TIn, TOut> Operation)
    : Node<TOut>
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);
    public override Ast.INode<TOut> Lower() =>
        new Ast.Chain.Sync<TIn, TOut>(Upstream.Lower(), Operation);
}
