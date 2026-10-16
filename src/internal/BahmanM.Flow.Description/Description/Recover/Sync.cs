namespace BahmanM.Flow.Description.Recover;

internal sealed record Sync<T>(
    IDescription<T> Source,
    BahmanM.Flow.Shared.Operations.Recover.Sync<T> Recover)
    : Node<T>
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);
    public override Ast.INode<T> Lower(Ast.IBehaviourStrategyFactory factory) =>
        factory.CreateRecoverSync(Source.Lower(factory), Recover);
}
