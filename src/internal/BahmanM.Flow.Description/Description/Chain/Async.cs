namespace BahmanM.Flow.Description.Chain;

internal sealed record Async<TIn, TOut>(
    IDescription<TIn> Upstream,
    BahmanM.Flow.Shared.Operations.Chain.Async<TIn, TOut> Operation)
    : Node<TOut>
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);
    public override Ast.INode<TOut> Lower(Ast.IBehaviourStrategyFactory factory) =>
        new Ast.Chain.Async<TIn, TOut>(Upstream.Lower(factory), Operation);
}
