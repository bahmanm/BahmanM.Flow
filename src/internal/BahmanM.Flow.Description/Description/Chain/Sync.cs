namespace BahmanM.Flow.Description.Chain;

internal sealed record Sync<TIn, TOut>(
    IDescription<TIn> Upstream,
    BahmanM.Flow.Shared.Operations.Chain.Sync<TIn, TOut> Operation)
    : Node<TOut>
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);
    public override Ast.INode<TOut> Lower(Ast.IBehaviourStrategyFactory factory) =>
        new Ast.Chain.Sync<TIn, TOut>(Upstream.Lower(factory), Operation);
}
