namespace BahmanM.Flow.Description.Recover;

internal sealed record Async<T>(
    IDescription<T> Source,
    BahmanM.Flow.Shared.Operations.Recover.Async<T> Recover)
    : Node<T>
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);
    public override Ast.INode<T> Lower(Ast.IBehaviourStrategyFactory factory) =>
        factory.CreateRecoverAsync(Source.Lower(factory), Recover);
}
