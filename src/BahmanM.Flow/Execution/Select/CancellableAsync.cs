using BahmanM.Flow.Support;

namespace BahmanM.Flow.Execution.Select;

internal record CancellableAsync(Ast.IInterpreter Interpreter, Options Options)
{
    internal async Task<Outcome<TOut>> Interpret<TIn, TOut>(Ast.Select.CancellableAsync<TIn, TOut> node)
    {
        var upstreamOutcome = await node.Upstream.AsNode().Accept(Interpreter);

        return upstreamOutcome switch
        {
            Success<TIn> s => await TryOperation.CancellableAsync(ct => node.Operation(s.Value, ct), Options.CancellationToken),
            Failure<TIn> f => Outcome.Failure<TOut>(f.Exception),
            _ => throw new NotSupportedException($"Unsupported outcome type: {upstreamOutcome.GetType().Name}")
        };
    }
}
