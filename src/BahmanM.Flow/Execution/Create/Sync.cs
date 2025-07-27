namespace BahmanM.Flow.Execution.Create;

internal class Sync(Ast.IInterpreter interpreter, Options options)
{
    internal Task<Outcome<T>> Interpret<T>(Ast.Create.Sync<T> node) =>
        TryOperation.Sync<T>(() => node.Operation());
}
