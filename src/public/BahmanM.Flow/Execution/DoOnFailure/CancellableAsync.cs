using BahmanM.Flow.Support;

namespace BahmanM.Flow.Execution.DoOnFailure;

internal class CancellableAsync(Ast.IInterpreter interpreter, Options options)
{
    internal async Task<Outcome<T>> Interpret<T>(Ast.DoOnFailure.CancellableAsync<T> node)
    {
        var upstreamOutcome = await node.Upstream.AsNode().Accept(interpreter);

        if (upstreamOutcome is Failure<T> failure)
        {
            try
            {
                await node.AsyncAction(failure.Exception, options.CancellationToken);
            }
            catch
            {
                /* Ignore */
            }
        }

        return upstreamOutcome;
    }

}
