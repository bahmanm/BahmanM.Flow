namespace BahmanM.Flow.Description.DoOnFailure;

internal sealed record CancellableAsync<T>(
    IDescription<T> Upstream,
    BahmanM.Flow.Shared.Operations.DoOnFailure.CancellableAsync AsyncAction)
    : Node<T>
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);

    public override Ast.INode<T> Lower(Ast.IBehaviourStrategyFactory factory) =>
        new Ast.DoOnFailure.CancellableAsync<T>(Upstream.Lower(factory), AsyncAction);
}
