namespace BahmanM.Flow.Description.Create;

internal sealed record CancellableAsync<T>(Flow.Operations.Create.CancellableAsync<T> Operation)
    : Node<T>("Create.CancellableAsync"), IOperation
{
    Delegate IOperation.Operation => Operation;

    public override Ast.INode<T> Lower() =>
        new Ast.Create.CancellableAsync<T>(cancellationToken => Operation(cancellationToken));
}
