namespace BahmanM.Flow.Execution.Primitive;

internal class Succeed(Ast.IInterpreter interpreter, Options options)
{
    internal Task<Outcome<T>> Interpret<T>(Ast.Primitive.Succeed<T> node) =>
        Task.FromResult(Outcome.Success(node.Value));
}
