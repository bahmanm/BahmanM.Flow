using BahmanM.Flow.Description.Serialisation;

namespace BahmanM.Flow.Description.Select;

internal sealed record Sync<TIn, TOut>(
    IDescription<TIn> Upstream,
    Flow.Operations.Select.Sync<TIn, TOut> Operation)
    : Node<TOut>("Select.Sync"), IOperation
{
    public override string OperatorDisplayName =>
        $"Select.Sync<{TypeFormatter.Format(typeof(TIn))}, {TypeFormatter.Format(typeof(TOut))}>";

    Delegate IOperation.Operation => Operation;
    public override IDescription? Source => Upstream;

    public override Ast.INode<TOut> Lower() =>
        new Ast.Select.Sync<TIn, TOut>(Upstream.Lower(), Operation);
}
