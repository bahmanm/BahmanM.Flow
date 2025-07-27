namespace BahmanM.Flow.Execution.Create;

internal class Async(Ast.IInterpreter interpreter, Options options)
{
    internal Task<Outcome<T>> Interpret<T>(Ast.Create.Async<T> node) =>
        TryOperation.Async<T>(() => node.Operation());
}
