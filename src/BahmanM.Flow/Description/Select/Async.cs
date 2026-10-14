using BahmanM.Flow.Description.Serialisation;

namespace BahmanM.Flow.Description.Select;

internal sealed record Async<TIn, TOut>(
    IDescription<TIn> Upstream,
    Flow.Operations.Select.Async<TIn, TOut> Operation)
    : Node<TOut>("Select.Async"), IOperation
{
    public override string OperatorDisplayName =>
        $"Select.Async<{TypeFormatter.Format(typeof(TIn))}, {TypeFormatter.Format(typeof(TOut))}>";

    Delegate IOperation.Operation => Operation;
    public override IDescription? Source => Upstream;

    public override Ast.INode<TOut> Lower() =>
        new Ast.Select.Async<TIn, TOut>(Upstream.Lower(), Operation);
}
