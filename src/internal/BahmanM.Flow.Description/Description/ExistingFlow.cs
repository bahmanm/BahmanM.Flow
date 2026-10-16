using BahmanM.Flow.Support;

namespace BahmanM.Flow.Description;

internal sealed record ExistingFlow<T>(IFlow<T> Flow) : Node<T>
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);
    public override Ast.INode<T> Lower(Ast.IBehaviourStrategyFactory factory) => Flow.AsNode(factory);
}
