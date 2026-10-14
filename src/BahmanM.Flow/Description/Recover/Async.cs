namespace BahmanM.Flow.Description.Recover;

internal sealed record Async<T>(
    IDescription<T> Source,
    Flow.Operations.Recover.Async<T> Recover)
    : Node<T>
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);
    public override Ast.INode<T> Lower() =>
        new Ast.Recover.Async<T>(Source.Lower(), Recover);
}
