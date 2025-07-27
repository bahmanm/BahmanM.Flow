using BahmanM.Flow.Execution.Select;

namespace BahmanM.Flow.Execution;

internal class Interpreter : Ast.IInterpreter
{
    private Options Options { get; init; }
    private NodeInterpreters NodeInterpreters { get; init; }

    public Interpreter() : this(new(CancellationToken: CancellationToken.None))
    {
    }

    public Interpreter(Options options)
    {
        Options = options;
        NodeInterpreters = new(this, Options);
    }

    public Task<Outcome<T>> Interpret<T>(Ast.Create.Sync<T> node) => NodeInterpreters.Create.Sync.Interpret(node);
    public Task<Outcome<T>> Interpret<T>(Ast.Create.Async<T> node) => NodeInterpreters.Create.Async.Interpret(node);
    public Task<Outcome<T>> Interpret<T>(Ast.Create.CancellableAsync<T> node) => NodeInterpreters.Create.CancellableAsync.Interpret(node);

    public Task<Outcome<T>> Interpret<T>(Ast.DoOnSuccess.Sync<T> node) => NodeInterpreters.DoOnSuccess.Sync.Interpret(node);
    public Task<Outcome<T>> Interpret<T>(Ast.DoOnSuccess.Async<T> node) => NodeInterpreters.DoOnSuccess.Async.Interpret(node);
    public Task<Outcome<T>> Interpret<T>(Ast.DoOnSuccess.CancellableAsync<T> node) => NodeInterpreters.DoOnSuccess.CancellableAsync.Interpret(node);

    public Task<Outcome<T>> Interpret<T>(Ast.DoOnFailure.Sync<T> node) => NodeInterpreters.DoOnFailure.Sync.Interpret(node);
    public Task<Outcome<T>> Interpret<T>(Ast.DoOnFailure.Async<T> node) => NodeInterpreters.DoOnFailure.Async.Interpret(node);
    public Task<Outcome<T>> Interpret<T>(Ast.DoOnFailure.CancellableAsync<T> node) => NodeInterpreters.DoOnFailure.CancellableAsync.Interpret(node);

    public Task<Outcome<TOut>> Interpret<TIn, TOut>(Ast.Select.Sync<TIn, TOut> node) => NodeInterpreters.Select.Sync.Interpret(node);
    public Task<Outcome<TOut>> Interpret<TIn, TOut>(Ast.Select.Async<TIn, TOut> node) => NodeInterpreters.Select.Async.Interpret(node);
    public Task<Outcome<TOut>> Interpret<TIn, TOut>(Ast.Select.CancellableAsync<TIn, TOut> node) => NodeInterpreters.Select.CancellableAsync.Interpret(node);

    public Task<Outcome<TOut>> Interpret<TIn, TOut>(Ast.Chain.Sync<TIn, TOut> node) => NodeInterpreters.Chain.Sync.Interpret(node);
    public Task<Outcome<TOut>> Interpret<TIn, TOut>(Ast.Chain.Async<TIn, TOut> node) => NodeInterpreters.Chain.Async.Interpret(node);
    public Task<Outcome<TOut>> Interpret<TIn, TOut>(Ast.Chain.CancellableAsync<TIn, TOut> node) => NodeInterpreters.Chain.CancellableAsync.Interpret(node);

    public Task<Outcome<T[]>> Interpret<T>(Ast.Primitive.All<T> node) => NodeInterpreters.Primitives.All.Interpret(node);
    public Task<Outcome<T>> Interpret<T>(Ast.Primitive.Any<T> node) => NodeInterpreters.Primitives.Any.Interpret(node);
    public Task<Outcome<T>> Interpret<T>(Ast.Primitive.Succeed<T> node) => NodeInterpreters.Primitives.Succeed.Interpret(node);
    public Task<Outcome<T>> Interpret<T>(Ast.Primitive.Fail<T> node) => NodeInterpreters.Primitives.Fail.Interpret(node);
}
