namespace BahmanM.Flow.Execution.Validate;

internal class Async(Ast.IInterpreter interpreter, Options options)
{
    internal async Task<Outcome<T>> Interpret<T>(Ast.Validate.Async<T> node)
    {
        var upstreamOutcome = await ((Ast.INode<T>)node.Upstream).Accept(interpreter);

        return upstreamOutcome switch
        {
            Success<T> s => await TryOperation.Async(async () =>
            {
                var ok = await node.PredicateAsync(s.Value);
                if (ok)
                {
                    return s.Value;
                }
                throw node.ExceptionFactory(s.Value);
            }),
            Failure<T> f => Outcome.Failure<T>(f.Exception),
            _ => throw new NotSupportedException($"Unsupported outcome type: {upstreamOutcome.GetType().Name}")
        };
    }
}

