namespace BahmanM.Flow.Ast;

internal interface IInterpreter
{
    internal Task<Outcome<T>> Interpret<T>(Create.Sync<T> node);
    internal Task<Outcome<T>> Interpret<T>(Create.Async<T> node);
    internal Task<Outcome<T>> Interpret<T>(Create.CancellableAsync<T> node);

    internal Task<Outcome<T>> Interpret<T>(DoOnSuccess.Sync<T> node);
    internal Task<Outcome<T>> Interpret<T>(DoOnSuccess.Async<T> node);
    internal Task<Outcome<T>> Interpret<T>(DoOnSuccess.CancellableAsync<T> node);

    internal Task<Outcome<T>> Interpret<T>(DoOnFailure.Sync<T> node);
    internal Task<Outcome<T>> Interpret<T>(DoOnFailure.Async<T> node);
    internal Task<Outcome<T>> Interpret<T>(DoOnFailure.CancellableAsync<T> node);

    internal Task<Outcome<TOut>> Interpret<TIn, TOut>(Select.Sync<TIn, TOut> node);
    internal Task<Outcome<TOut>> Interpret<TIn, TOut>(Select.Async<TIn, TOut> node);
    internal Task<Outcome<TOut>> Interpret<TIn, TOut>(Select.CancellableAsync<TIn, TOut> node);

    internal Task<Outcome<TOut>> Interpret<TIn, TOut>(Chain.Sync<TIn, TOut> node);
    internal Task<Outcome<TOut>> Interpret<TIn, TOut>(Chain.Async<TIn, TOut> node);
    internal Task<Outcome<TOut>> Interpret<TIn, TOut>(Chain.CancellableAsync<TIn, TOut> node);

    internal Task<Outcome<T[]>> Interpret<T>(Primitive.All<T> node);
    internal Task<Outcome<T>> Interpret<T>(Primitive.Any<T> node);
    internal Task<Outcome<T>> Interpret<T>(Primitive.Succeed<T> node);
    internal Task<Outcome<T>> Interpret<T>(Primitive.Fail<T> node);
}
