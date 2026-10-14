namespace BahmanM.Flow.Description.DoOnSuccess;

internal sealed record CancellableAsync<T>(
    IDescription<T> Upstream,
    Flow.Operations.DoOnSuccess.CancellableAsync<T> AsyncAction)
    : Node<T>
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);

    public override Ast.INode<T> Lower() =>
        new Ast.DoOnSuccess.CancellableAsync<T>(Upstream.Lower(), AsyncAction);
}
