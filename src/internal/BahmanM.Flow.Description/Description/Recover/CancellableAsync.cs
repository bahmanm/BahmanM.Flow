namespace BahmanM.Flow.Description.Recover;

internal sealed record CancellableAsync<T>(
    IDescription<T> Source,
    Func<Exception, CancellationToken, Task<IFlow<T>>> Recover)
    : Node<T>
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);
    public override Ast.INode<T> Lower(Ast.IBehaviourStrategyFactory factory) =>
        factory.CreateRecoverCancellableAsync(
            Source.Lower(factory),
            (error, cancellationToken) => Recover(error, cancellationToken));
}
