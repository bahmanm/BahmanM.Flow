namespace BahmanM.Flow.Description.Chain;

internal sealed record CancellableAsync<TIn, TOut>(
    IDescription<TIn> Upstream,
    BahmanM.Flow.Shared.Operations.Chain.CancellableAsync<TIn, TOut> Operation)
    : Node<TOut>
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);
    public override Ast.INode<TOut> Lower(Ast.IBehaviourStrategyFactory factory) =>
        new Ast.Chain.CancellableAsync<TIn, TOut>(
            Upstream.Lower(factory),
            (value, options) => Operation(value, options.CancellationToken));
}
