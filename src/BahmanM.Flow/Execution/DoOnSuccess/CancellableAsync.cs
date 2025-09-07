using BahmanM.Flow.Support;

namespace BahmanM.Flow.Execution.DoOnSuccess;

internal class CancellableAsync(Ast.IInterpreter interpreter, Options options)
{
    internal async Task<Outcome<T>> Interpret<T>(Ast.DoOnSuccess.CancellableAsync<T> node)
    {
        var upstreamOutcome = await node.Upstream.AsNode().Accept(interpreter);

        if (upstreamOutcome is Success<T> success)
        {
            try
            {
                await node.AsyncAction(success.Value, options.CancellationToken);
                return upstreamOutcome;
            }
            catch (Exception ex)
            {
                return Outcome.Failure<T>(ex);
            }
        }

        return upstreamOutcome;
    }
}
