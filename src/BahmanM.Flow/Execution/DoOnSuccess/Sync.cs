namespace BahmanM.Flow.Execution.DoOnSuccess;

internal class Sync(Ast.IInterpreter interpreter, Options options)
{
    internal async Task<Outcome<T>> Interpret<T>(Ast.DoOnSuccess.Sync<T> node)
    {
        var upstreamOutcome = await ((Ast.INode<T>)node.Upstream).Accept(interpreter);

        if (upstreamOutcome is Success<T> success)
        {
            try
            {
                node.Action(success.Value);
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
