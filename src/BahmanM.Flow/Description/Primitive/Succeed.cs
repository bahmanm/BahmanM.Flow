namespace BahmanM.Flow.Description.Primitive;

internal sealed record Succeed<T>(T Value) : Node<T>("Succeed"), IValue
{
    object? IValue.Value => Value;

    public override Ast.INode<T> Lower() => new Ast.Primitive.Succeed<T>(Value);
}
