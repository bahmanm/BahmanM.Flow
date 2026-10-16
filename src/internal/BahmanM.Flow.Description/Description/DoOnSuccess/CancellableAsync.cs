namespace BahmanM.Flow.Description.DoOnSuccess;

internal sealed record CancellableAsync<T>(
    IDescription<T> Upstream,
    BahmanM.Flow.Shared.Operations.DoOnSuccess.CancellableAsync<T> AsyncAction)
    : Node<T>
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);

    public override Ast.INode<T> Lower(Ast.IBehaviourStrategyFactory factory) =>
        new Ast.DoOnSuccess.CancellableAsync<T>(Upstream.Lower(factory), AsyncAction);
}
