using BahmanM.Flow.Description.Serialisation;

namespace BahmanM.Flow.Description.Select;

internal sealed record Async<TIn, TOut>(
    IDescription<TIn> Upstream,
    Flow.Operations.Select.Async<TIn, TOut> Operation)
    : Node<TOut>
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);
    public override Ast.INode<TOut> Lower() =>
        new Ast.Select.Async<TIn, TOut>(Upstream.Lower(), Operation);
}
