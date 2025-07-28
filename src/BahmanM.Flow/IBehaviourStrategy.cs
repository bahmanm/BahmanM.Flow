namespace BahmanM.Flow;

internal interface IBehaviourStrategy<TValue>
{
    IFlow<TValue> ApplyTo(Ast.Primitive.Succeed<TValue> node);
    IFlow<TValue> ApplyTo(Ast.Primitive.Fail<TValue> node);
    IFlow<TValue> ApplyTo(Ast.Create.Sync<TValue> node);
    IFlow<TValue> ApplyTo(Ast.Create.Async<TValue> node);
    IFlow<TValue> ApplyTo(Ast.Create.CancellableAsync<TValue> node);
    IFlow<TValue> ApplyTo(Ast.DoOnSuccess.Sync<TValue> node);
    IFlow<TValue> ApplyTo(Ast.DoOnSuccess.Async<TValue> node);
    IFlow<TValue> ApplyTo(Ast.DoOnSuccess.CancellableAsync<TValue> node);
    IFlow<TValue> ApplyTo(Ast.DoOnFailure.Sync<TValue> node);
    IFlow<TValue> ApplyTo(Ast.DoOnFailure.Async<TValue> node);
    IFlow<TValue> ApplyTo(Ast.DoOnFailure.CancellableAsync<TValue> node);
    IFlow<TValue> ApplyTo<TIn>(Ast.Select.Sync<TIn, TValue> node);
    IFlow<TValue> ApplyTo<TIn>(Ast.Select.Async<TIn, TValue> node);
    IFlow<TValue> ApplyTo<TIn>(Ast.Select.CancellableAsync<TIn, TValue> node);
    IFlow<TValue> ApplyTo<TIn>(Ast.Chain.Sync<TIn, TValue> node);
    IFlow<TValue> ApplyTo<TIn>(Ast.Chain.Async<TIn, TValue> node);
    IFlow<TValue> ApplyTo<TIn>(Ast.Chain.CancellableAsync<TIn, TValue> node);
    IFlow<IList<TValue>> ApplyTo(Ast.Primitive.All<TValue> node);
    IFlow<TValue> ApplyTo(Ast.Primitive.Any<TValue> node);
}
