namespace BahmanM.Flow.Execution.Primitive;

internal class Fail(Ast.IInterpreter interpreter, Options options)
{
    internal Task<Outcome<T>> Interpret<T>(Ast.Primitive.Fail<T> node) =>
        Task.FromResult(Outcome.Failure<T>(node.Exception));
}
