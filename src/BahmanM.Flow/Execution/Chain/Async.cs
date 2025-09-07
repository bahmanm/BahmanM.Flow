using BahmanM.Flow.Support;

namespace BahmanM.Flow.Execution.Chain;

internal class Async(Ast.IInterpreter interpreter, Options options)
{
    internal async Task<Outcome<TOut>> Interpret<TIn, TOut>(Ast.Chain.Async<TIn, TOut> node)
    {
        var upstreamOutcome = await node.Upstream.AsNode().Accept(interpreter);

        if (upstreamOutcome is not Success<TIn> success)
        {
            return Outcome.Failure<TOut>(((Failure<TIn>)upstreamOutcome).Exception);
        }

        try
        {
            var nextFlow = (await node.Operation(success.Value)).AsNode();
            return await nextFlow.Accept(interpreter);
        }
        catch (Exception ex)
        {
            return Outcome.Failure<TOut>(ex);
        }
    }
}
