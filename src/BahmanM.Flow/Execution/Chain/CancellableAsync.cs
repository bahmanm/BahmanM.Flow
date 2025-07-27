namespace BahmanM.Flow.Execution.Chain;

internal class CancellableAsync(Ast.IInterpreter interpreter, Options options)
{
    internal async Task<Outcome<TOut>> Interpret<TIn, TOut>(Ast.Chain.CancellableAsync<TIn, TOut> node)
    {
        var upstreamOutcome = await ((Ast.INode<TIn>)node.Upstream).Accept(interpreter);

        if (upstreamOutcome is not Success<TIn> success)
        {
            return Outcome.Failure<TOut>(((Failure<TIn>)upstreamOutcome).Exception);
        }

        try
        {
            if (options.CancellationToken.IsCancellationRequested)
            {
                return Outcome.Failure<TOut>(new TaskCanceledException());
            }

            var nextFlow = (Ast.INode<TOut>)await node.Operation(success.Value, options.CancellationToken);
            return await nextFlow.Accept(interpreter);
        }
        catch (Exception ex)
        {
            return Outcome.Failure<TOut>(ex);
        }
    }

}
