namespace BahmanM.Flow.Description.DoOnFailure;

internal sealed record Sync<T>(
    IDescription<T> Upstream,
    Flow.Operations.DoOnFailure.Sync Action)
    : Node<T>
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);
    public override Ast.INode<T> Lower() =>
        new Ast.DoOnFailure.Sync<T>(Upstream.Lower(), Action);
}
