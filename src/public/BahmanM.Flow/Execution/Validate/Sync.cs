using BahmanM.Flow.Support;

namespace BahmanM.Flow.Execution.Validate;

internal class Sync(Ast.IInterpreter interpreter, Options options)
{
    internal async Task<Outcome<T>> Interpret<T>(Ast.Validate.Sync<T> node)
    {
        var upstreamOutcome = await node.Upstream.AsNode().Accept(interpreter);

        return upstreamOutcome switch
        {
            Success<T> s => await TryOperation.Sync(() =>
            {
                if (node.Predicate(s.Value))
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
