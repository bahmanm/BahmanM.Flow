using BahmanM.Flow.Description.Serialisation;

namespace BahmanM.Flow.Description.Select;

internal sealed record Sync<TIn, TOut>(
    IDescription<TIn> Upstream,
    Flow.Operations.Select.Sync<TIn, TOut> Operation)
    : Node<TOut>
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);
    public override Ast.INode<TOut> Lower() =>
        new Ast.Select.Sync<TIn, TOut>(Upstream.Lower(), Operation);
}
