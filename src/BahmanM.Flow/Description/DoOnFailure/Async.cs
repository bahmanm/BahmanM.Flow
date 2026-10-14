namespace BahmanM.Flow.Description.DoOnFailure;

internal sealed record Async<T>(
    IDescription<T> Upstream,
    Flow.Operations.DoOnFailure.Async AsyncAction)
    : Node<T>
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);

    public override Ast.INode<T> Lower() =>
        new Ast.DoOnFailure.Async<T>(Upstream.Lower(), AsyncAction);
}
