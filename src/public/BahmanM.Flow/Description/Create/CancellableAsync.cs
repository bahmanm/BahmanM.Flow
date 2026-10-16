namespace BahmanM.Flow.Description.Create;

internal sealed record CancellableAsync<T>(Flow.Operations.Create.CancellableAsync<T> Operation)
    : Node<T>
{
    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);
    public override Ast.INode<T> Lower() =>
        new Ast.Create.CancellableAsync<T>(options => Operation(options.CancellationToken));
}
