namespace BahmanM.Flow.Description.Create;

internal sealed record Sync<T>(Flow.Operations.Create.Sync<T> Operation)
    : Node<T>
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);
    public override Ast.INode<T> Lower() => new Ast.Create.Sync<T>(Operation);
}
