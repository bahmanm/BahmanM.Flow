namespace BahmanM.Flow.Description.DoOnSuccess;

internal sealed record Async<T>(
    IDescription<T> Upstream,
    Flow.Operations.DoOnSuccess.Async<T> AsyncAction)
    : Node<T>
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);

    public override Ast.INode<T> Lower() =>
        new Ast.DoOnSuccess.Async<T>(Upstream.Lower(), AsyncAction);
}
