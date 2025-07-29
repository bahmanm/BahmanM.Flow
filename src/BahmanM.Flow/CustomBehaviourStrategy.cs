namespace BahmanM.Flow;

internal class CustomBehaviourStrategy(IBehaviour behaviour) : IBehaviourStrategy
{
    public IFlow<T> ApplyTo<T>(Ast.Primitive.Succeed<T> node) => behaviour.Apply(node);
    public IFlow<T> ApplyTo<T>(Ast.Primitive.Fail<T> node) => behaviour.Apply(node);
    public IFlow<T> ApplyTo<T>(Ast.Create.Sync<T> node) => behaviour.Apply(node);
    public IFlow<T> ApplyTo<T>(Ast.Create.Async<T> node) => behaviour.Apply(node);
    public IFlow<T> ApplyTo<T>(Ast.Create.CancellableAsync<T> node) => behaviour.Apply(node);
    public IFlow<T> ApplyTo<T>(Ast.DoOnSuccess.Sync<T> node) => behaviour.Apply(node);
    public IFlow<T> ApplyTo<T>(Ast.DoOnSuccess.Async<T> node) => behaviour.Apply(node);
    public IFlow<T> ApplyTo<T>(Ast.DoOnSuccess.CancellableAsync<T> node) => behaviour.Apply(node);
    public IFlow<T> ApplyTo<T>(Ast.DoOnFailure.Sync<T> node) => behaviour.Apply(node);
    public IFlow<T> ApplyTo<T>(Ast.DoOnFailure.Async<T> node) => behaviour.Apply(node);
    public IFlow<T> ApplyTo<T>(Ast.DoOnFailure.CancellableAsync<T> node) => behaviour.Apply(node);
    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Select.Sync<TIn, TOut> node) => behaviour.Apply(node);
    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Select.Async<TIn, TOut> node) => behaviour.Apply(node);
    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Select.CancellableAsync<TIn, TOut> node) => behaviour.Apply(node);
    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Chain.Sync<TIn, TOut> node) => behaviour.Apply(node);
    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Chain.Async<TIn, TOut> node) => behaviour.Apply(node);
    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Chain.CancellableAsync<TIn, TOut> node) => behaviour.Apply(node);
    public IFlow<T[]> ApplyTo<T>(Ast.Primitive.All<T> node) => behaviour.Apply(node);
    public IFlow<T> ApplyTo<T>(Ast.Primitive.Any<T> node) => behaviour.Apply(node);
}
