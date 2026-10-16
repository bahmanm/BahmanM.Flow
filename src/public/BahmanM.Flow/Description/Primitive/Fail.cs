namespace BahmanM.Flow.Description.Primitive;

internal sealed record Fail<T>(Exception Exception) : Node<T>
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);
    public override Ast.INode<T> Lower() => new Ast.Primitive.Fail<T>(Exception);
}
