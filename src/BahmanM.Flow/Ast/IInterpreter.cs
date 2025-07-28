using BahmanM.Flow.Ast.Primitive;

namespace BahmanM.Flow.Ast;

internal interface IInterpreter<TValue,TResult> :
    ICreateInterpreter<TValue,TResult>,
    IDoOnSuccessInterpreter<TValue,TResult>,
    IDoOnFailureInterpreter<TValue,TResult>,
    ISelectInterpreter<TValue,TResult>,
    IChainInterpreter<TValue,TResult>,
    IPrimitiveInterpreter<TValue,TResult>,
    IApplicativeInterpreter<TValue, Task<Outcome<IList<TValue>>>>,
    IAlternativeInterpreter<TValue,TResult>
{
}

internal interface ICreateInterpreter<TValue,TResult>
{
    internal TResult Interpret(Ast.Create.Sync<TValue> node);
    internal TResult Interpret(Ast.Create.Async<TValue> node);
    internal TResult Interpret(Ast.Create.CancellableAsync<TValue> node);
}

internal interface IDoOnSuccessInterpreter<TValue,TResult>
{
    internal TResult Interpret(Ast.DoOnSuccess.Sync<TValue> node);
    internal TResult Interpret(Ast.DoOnSuccess.Async<TValue> node);
    internal TResult Interpret(Ast.DoOnSuccess.CancellableAsync<TValue> node);
}

internal interface IDoOnFailureInterpreter<TValue,  TResult>
{
    internal TResult Interpret(Ast.DoOnFailure.Sync<TValue> node);
    internal TResult Interpret(Ast.DoOnFailure.Async<TValue> node);
    internal TResult Interpret(Ast.DoOnFailure.CancellableAsync<TValue> node);
}

internal interface ISelectInterpreter<TValue, TResult>
{
    internal TResult Interpret<TLastValue>(Ast.Select.Sync<TLastValue, TValue> node);
    internal TResult Interpret<TLastValue>(Ast.Select.Async<TLastValue, TValue> node);
    internal TResult Interpret<TLastValue>(Ast.Select.CancellableAsync<TLastValue, TValue> node);
}

internal interface IChainInterpreter<TValue, TResult>
{
    internal TResult Interpret<TLastValue>(Ast.Chain.Sync<TLastValue, TValue> node);
    internal TResult Interpret<TLastValue>(Ast.Chain.Async<TLastValue, TValue> node);
    internal TResult Interpret<TLastValue>(Ast.Chain.CancellableAsync<TLastValue, TValue> node);
}

internal interface IPrimitiveInterpreter<TValue, TResult>
{
    internal TResult Interpret(Ast.Primitive.Succeed<TValue> node);
    internal TResult Interpret(Ast.Primitive.Fail<TValue> node);
}

internal interface IApplicativeInterpreter<TValue, TResult>
{
    Task<Outcome<IList<TValue>>> Interpret(Ast.Primitive.All<TValue> node);
}

internal interface IAlternativeInterpreter<TValue, TResult>
{
    TResult Interpret(Ast.Primitive.Any<TValue> node);
}
