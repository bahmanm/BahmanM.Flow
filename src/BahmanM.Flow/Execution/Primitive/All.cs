namespace BahmanM.Flow.Execution.Primitive;

internal class All(Ast.IInterpreter interpreter, Options options)
{
    internal async Task<Outcome<T[]>> Interpret<T>(Ast.Primitive.All<T> node)
    {
        // This implementation waits for all flows to complete. If any have failed, it
        // aggregates all their exceptions. This provides the most comprehensive diagnostic
        // information to the caller, rather than failing fast on the first exception.
        var outcomes = await Task.WhenAll(node.Flows.Select(f => ((Ast.INode<T>)f).Accept(interpreter)));

        var exceptions = outcomes.OfType<Failure<T>>().Select(f => f.Exception).ToList();

        return exceptions is not []
            ? Outcome.Failure<T[]>(new AggregateException(exceptions))
            : Outcome.Success(outcomes.OfType<Success<T>>().Select(s => s.Value).ToArray());
    }
}
