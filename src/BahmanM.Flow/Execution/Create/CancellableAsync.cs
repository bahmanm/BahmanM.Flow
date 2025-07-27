namespace BahmanM.Flow.Execution.Create;

internal class CancellableAsync(Ast.IInterpreter interpreter, Options options)
{
    internal Task<Outcome<T>> Interpret<T>(Ast.Create.CancellableAsync<T> node) =>
        TryOperation.CancellableAsync<T>((cancellationToken) => node.Operation(cancellationToken),
            options.CancellationToken);
}
