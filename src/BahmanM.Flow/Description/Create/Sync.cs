namespace BahmanM.Flow.Description.Create;

internal sealed record Sync<T>(Flow.Operations.Create.Sync<T> Operation)
    : Node<T>("Create.Sync"), IOperation
{
    Delegate IOperation.Operation => Operation;

    public override Ast.INode<T> Lower() => new Ast.Create.Sync<T>(Operation);
}
