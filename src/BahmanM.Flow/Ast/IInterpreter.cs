namespace BahmanM.Flow.Ast;

internal interface IInterpreter<TResult> :
    ICreateInterpreter<TResult>,
    IDoOnSuccessInterpreter<TResult>,
    IDoOnFailureInterpreter<TResult>,
    ISelectInterpreter<TResult>,
    IChainInterpreter<TResult>,
    IPrimitiveInterpreter<TResult>,
    IApplicativeInterpreter<TResult>,
    IAlternativeInterpreter<TResult>
{
}

internal interface ICreateInterpreter<TResult>
{
    internal TResult Interpret<TValue>(Create.Sync<TValue> node);
    internal TResult Interpret<TValue>(Create.Async<TValue> node);
    internal TResult Interpret<TValue>(Create.CancellableAsync<TValue> node);
}

internal interface IDoOnSuccessInterpreter<TResult>
{
    internal TResult Interpret<TValue>(DoOnSuccess.Sync<TValue> node);
    internal TResult Interpret<TValue>(DoOnSuccess.Async<TValue> node);
    internal TResult Interpret<TValue>(DoOnSuccess.CancellableAsync<TValue> node);
}

internal interface IDoOnFailureInterpreter<TResult>
{
    internal TResult Interpret<TValue>(DoOnFailure.Sync<TValue> node);
    internal TResult Interpret<TValue>(DoOnFailure.Async<TValue> node);
    internal TResult Interpret<TValue>(DoOnFailure.CancellableAsync<TValue> node);
}

internal interface ISelectInterpreter<TResult>
{
    internal TResult Interpret<TIn, TOut>(Select.Sync<TIn, TOut> node);
    internal TResult Interpret<TIn, TOut>(Select.Async<TIn, TOut> node);
    internal TResult Interpret<TIn, TOut>(Select.CancellableAsync<TIn, TOut> node);
}

internal interface IChainInterpreter<TResult>
{
    internal TResult Interpret<TIn, TOut>(Chain.Sync<TIn, TOut> node);
    internal TResult Interpret<TIn, TOut>(Chain.Async<TIn, TOut> node);
    internal TResult Interpret<TIn, TOut>(Chain.CancellableAsync<TIn, TOut> node);
}

internal interface IPrimitiveInterpreter<TResult>
{
    internal TResult Interpret<TValue>(Primitive.Succeed<TValue> node);
    internal TResult Interpret<TValue>(Primitive.Fail<TValue> node);
}

internal interface IApplicativeInterpreter<TResult>
{
    Task<Outcome<TValue[]>> Interpret<TValue>(Primitive.All<TValue> node);
}

internal interface IAlternativeInterpreter<TResult>
{
    TResult Interpret<TValue>(Primitive.Any<TValue> node);
}
