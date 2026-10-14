namespace BahmanM.Flow.Description.Primitive;

internal sealed record Fail<T>(Exception Exception) : Node<T>("Fail"), IFailure
{
    public override Ast.INode<T> Lower() => new Ast.Primitive.Fail<T>(Exception);
}
