namespace BahmanM.Flow.Description.DoOnFailure;

internal sealed record Sync<T>(
    IDescription<T> Upstream,
    BahmanM.Flow.Shared.Operations.DoOnFailure.Sync Action)
    : Node<T>
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);
    public override Ast.INode<T> Lower(Ast.IBehaviourStrategyFactory factory) =>
        new Ast.DoOnFailure.Sync<T>(Upstream.Lower(factory), Action);
}
