namespace BahmanM.Flow.Execution.Primitive;

internal class Any(Ast.IInterpreter interpreter, Options options)
{
    internal async Task<Outcome<T>> Interpret<T>(Ast.Primitive.Any<T> node) =>
        await TryOperation.TryFindFirstSuccessfulFlow<T>(
            node.Flows.Select(f => ((Ast.INode<T>)f).Accept(interpreter)).ToList(),
            []);
}
