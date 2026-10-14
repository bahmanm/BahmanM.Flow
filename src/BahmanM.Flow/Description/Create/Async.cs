namespace BahmanM.Flow.Description.Create;

internal sealed record Async<T>(Flow.Operations.Create.Async<T> Operation)
    : Node<T>("Create.Async"), IOperation
{
    Delegate IOperation.Operation => Operation;

    public override Ast.INode<T> Lower() => new Ast.Create.Async<T>(Operation);
}
