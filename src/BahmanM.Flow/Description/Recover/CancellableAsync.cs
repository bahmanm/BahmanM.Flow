namespace BahmanM.Flow.Description.Recover;

internal sealed record CancellableAsync<T>(
    IDescription<T> Source,
    Func<Exception, CancellationToken, Task<IFlow<T>>> Recover)
    : Node<T>
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);
    public override Ast.INode<T> Lower() =>
        new Ast.Recover.CancellableAsync<T>(Source.Lower(), Recover);
}
