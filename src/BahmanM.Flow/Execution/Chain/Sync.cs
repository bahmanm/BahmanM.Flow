namespace BahmanM.Flow.Execution.Chain;

internal class Sync(Ast.IInterpreter interpreter, Options options)
{
    internal async Task<Outcome<TOut>> Interpret<TIn, TOut>(Ast.Chain.Sync<TIn, TOut> node)
    {
        var upstreamOutcome = await ((Ast.INode<TIn>)node.Upstream).Accept(interpreter);

        if (upstreamOutcome is not Success<TIn> success)
        {
            return Outcome.Failure<TOut>(((Failure<TIn>)upstreamOutcome).Exception);
        }

        try
        {
            var nextFlow = (Ast.INode<TOut>)node.Operation(success.Value);
            return await nextFlow.Accept(interpreter);
        }
        catch (Exception ex)
        {
            return Outcome.Failure<TOut>(ex);
        }
    }
}
