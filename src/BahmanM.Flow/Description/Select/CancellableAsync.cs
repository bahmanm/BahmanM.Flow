using BahmanM.Flow.Description.Serialisation;

namespace BahmanM.Flow.Description.Select;

internal sealed record CancellableAsync<TIn, TOut>(
    IDescription<TIn> Upstream,
    Flow.Operations.Select.CancellableAsync<TIn, TOut> Operation)
    : Node<TOut>("Select.CancellableAsync"), IOperation
{
    public override string OperatorDisplayName =>
        $"Select.CancellableAsync<{TypeFormatter.Format(typeof(TIn))}, {TypeFormatter.Format(typeof(TOut))}>";

    Delegate IOperation.Operation => Operation;
    public override IDescription? Source => Upstream;

    public override Ast.INode<TOut> Lower() =>
        new Ast.Select.CancellableAsync<TIn, TOut>(Upstream.Lower(), Operation);
}
