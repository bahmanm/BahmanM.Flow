using BahmanM.Flow.Support;

namespace BahmanM.Flow.Execution.Validate;

internal class CancellableAsync(Ast.IInterpreter interpreter, Options options)
{
    internal async Task<Outcome<T>> Interpret<T>(Ast.Validate.CancellableAsync<T> node)
    {
        var upstreamOutcome = await node.Upstream.AsNode().Accept(interpreter);

        return upstreamOutcome switch
        {
            Success<T> s => await TryOperation.CancellableAsync(async ct =>
            {
                var ok = await node.PredicateCancellableAsync(s.Value, ct);
                if (ok)
                {
                    return s.Value;
                }
                throw node.ExceptionFactory(s.Value);
            }, options.CancellationToken),
            Failure<T> f => Outcome.Failure<T>(f.Exception),
            _ => throw new NotSupportedException($"Unsupported outcome type: {upstreamOutcome.GetType().Name}")
        };
    }
}
