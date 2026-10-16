namespace BahmanM.Flow.Description.Create;

internal sealed record Async<T>(BahmanM.Flow.Shared.Operations.Create.Async<T> Operation)
    : Node<T>
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);
    public override Ast.INode<T> Lower(Ast.IBehaviourStrategyFactory factory) => new Ast.Create.Async<T>(Operation);
}
