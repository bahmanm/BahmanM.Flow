namespace BahmanM.Flow.Description.DoOnSuccess;

internal sealed record Sync<T>(
    IDescription<T> Upstream,
    BahmanM.Flow.Shared.Operations.DoOnSuccess.Sync<T> Action)
    : Node<T>
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);
    public override Ast.INode<T> Lower(Ast.IBehaviourStrategyFactory factory) =>
        new Ast.DoOnSuccess.Sync<T>(Upstream.Lower(factory), Action);
}
