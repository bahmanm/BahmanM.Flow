namespace BahmanM.Flow.Description.Primitive;

internal sealed record Succeed<T>(T Value) : Node<T>
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);
    public override Ast.INode<T> Lower() => new Ast.Primitive.Succeed<T>(Value);
}
