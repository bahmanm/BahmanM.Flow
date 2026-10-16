namespace BahmanM.Flow.Description.Recover;

internal sealed record Sync<T>(
    IDescription<T> Source,
    Flow.Operations.Recover.Sync<T> Recover)
    : Node<T>
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);
    public override Ast.INode<T> Lower() =>
        new Ast.Recover.Sync<T>(Source.Lower(), Recover);
}
