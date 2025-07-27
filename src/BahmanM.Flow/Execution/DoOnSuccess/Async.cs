namespace BahmanM.Flow.Execution.DoOnSuccess;

internal class Async(Ast.IInterpreter interpreter, Options options)
{
    internal async Task<Outcome<T>> Interpret<T>(Ast.DoOnSuccess.Async<T> node)
    {
        var upstreamOutcome = await ((Ast.INode<T>)node.Upstream).Accept(interpreter);

        if (upstreamOutcome is Success<T> success)
        {
            try
            {
                await node.AsyncAction(success.Value);
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
