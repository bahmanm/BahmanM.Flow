namespace BahmanM.Flow.Execution.Select;

internal class Async(Ast.IInterpreter interpreter, Options options)
{
    internal async Task<Outcome<TOut>> Interpret<TIn, TOut>(Ast.Select.Async<TIn, TOut> node)
    {
        var upstreamOutcome = await ((Ast.INode<TIn>)node.Upstream).Accept(interpreter);

        return upstreamOutcome switch
        {
            Success<TIn> s => await TryOperation.Async(() => node.Operation(s.Value)),
            Failure<TIn> f => Outcome.Failure<TOut>(f.Exception),
            _ => throw new NotSupportedException($"Unsupported outcome type: {upstreamOutcome.GetType().Name}")
        };
    }
}
