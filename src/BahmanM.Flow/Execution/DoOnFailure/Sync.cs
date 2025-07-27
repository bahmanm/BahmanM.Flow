namespace BahmanM.Flow.Execution.DoOnFailure;

internal class Sync(Ast.IInterpreter interpreter, Options options)
{
    internal async Task<Outcome<T>> Interpret<T>(Ast.DoOnFailure.Sync<T> node)
    {
        var upstreamOutcome = await ((Ast.INode<T>)node.Upstream).Accept(interpreter);

        if (upstreamOutcome is Failure<T> failure)
        {
            try
            {
                node.Action(failure.Exception);
            }
            catch
            {
                /* Ignore */
            }
        }

        return upstreamOutcome;
    }
}
