namespace BahmanM.Flow.Description.Chain;

internal sealed record CancellableAsync<TIn, TOut>(
    IDescription<TIn> Upstream,
    Flow.Operations.Chain.CancellableAsync<TIn, TOut> Operation)
    : Node<TOut>
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);
    public override Ast.INode<TOut> Lower() =>
        new Ast.Chain.CancellableAsync<TIn, TOut>(
            Upstream.Lower(),
            (value, options) => Operation(value, options.CancellationToken));
}
