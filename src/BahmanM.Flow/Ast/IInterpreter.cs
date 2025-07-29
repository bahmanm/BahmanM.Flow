namespace BahmanM.Flow.Ast;

internal interface IInterpreter<T, out TResult> :
    ICreateInterpreter<T, TResult>,
        IDoOnSuccessInterpreter<T>,
        IDoOnFailureInterpreter<T>,
        ISelectInterpreter,
        IChainInterpreter,
        IPrimitiveInterpreter<T>,
        IApplicativeInterpreter,
        IAlternativeInterpreter<T>
{
}

internal interface ICreateInterpreter<T, out TResult>
{
    internal TResult Interpret(Create.Sync<T> node);
    internal TResult Interpret(Create.Async<T> node);
    internal TResult Interpret(Create.CancellableAsync<T> node);
}

internal interface IDoOnSuccessInterpreter<T>
{
    internal Task<Outcome<T>> Interpret(DoOnSuccess.Sync<T> node);
    internal Task<Outcome<T>> Interpret(DoOnSuccess.Async<T> node);
    internal Task<Outcome<T>> Interpret(DoOnSuccess.CancellableAsync<T> node);
}

internal interface IDoOnFailureInterpreter<T>
{
    internal Task<Outcome<T>> Interpret(DoOnFailure.Sync<T> node);
    internal Task<Outcome<T>> Interpret(DoOnFailure.Async<T> node);
    internal Task<Outcome<T>> Interpret(DoOnFailure.CancellableAsync<T> node);
}

internal interface ISelectInterpreter
{
    internal Task<Outcome<TOut>> Interpret<TIn, TOut>(Select.Sync<TIn, TOut> node);
    internal Task<Outcome<TOut>> Interpret<TIn, TOut>(Select.Async<TIn, TOut> node);
    internal Task<Outcome<TOut>> Interpret<TIn, TOut>(Select.CancellableAsync<TIn, TOut> node);
}

internal interface IChainInterpreter
{
    internal Task<Outcome<TOut>> Interpret<TIn, TOut>(Chain.Sync<TIn, TOut> node);
    internal Task<Outcome<TOut>> Interpret<TIn, TOut>(Chain.Async<TIn, TOut> node);
    internal Task<Outcome<TOut>> Interpret<TIn, TOut>(Chain.CancellableAsync<TIn, TOut> node);
}

internal interface IPrimitiveInterpreter<T>
{
    internal Task<Outcome<T>> Interpret(Primitive.Succeed<T> node);
    internal Task<Outcome<T>> Interpret(Primitive.Fail<T> node);
}

internal interface IApplicativeInterpreter
{
    Task<Outcome<T[]>> Interpret<T>(Primitive.All<T> node);
}

internal interface IAlternativeInterpreter<T>
{
    Task<Outcome<T>> Interpret(Primitive.Any<T> node);
}
