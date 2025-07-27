namespace BahmanM.Flow.Execution.DoOnFailure;

internal class Async(Ast.IInterpreter interpreter, Options options)
{
    internal async Task<Outcome<T>> Interpret<T>(Ast.DoOnFailure.Async<T> node)
    {
        var upstreamOutcome = await ((Ast.INode<T>)node.Upstream).Accept(interpreter);

        if (upstreamOutcome is Failure<T> failure)
        {
            try
            {
                await node.AsyncAction(failure.Exception);
            }
            catch
            {
                /* Ignore */
            }
        }

        return upstreamOutcome;
    }
}
