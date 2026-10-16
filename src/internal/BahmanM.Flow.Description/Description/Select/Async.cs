namespace BahmanM.Flow.Description.Select;

internal sealed record Async<TIn, TOut>(
    IDescription<TIn> Upstream,
    BahmanM.Flow.Shared.Operations.Select.Async<TIn, TOut> Operation)
    : Node<TOut>
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);
    public override Ast.INode<TOut> Lower(Ast.IBehaviourStrategyFactory factory) =>
        new Ast.Select.Async<TIn, TOut>(Upstream.Lower(factory), Operation);
}
