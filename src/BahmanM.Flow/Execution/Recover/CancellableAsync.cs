using BahmanM.Flow.Ast;
using BahmanM.Flow.Support;
using BahmanM.Flow.Ast.Recover;

namespace BahmanM.Flow.Execution.Recover;

internal static class CancellableAsync
{
    internal static async Task<Outcome<T>> Execute<T>(CancellableAsync<T> cancellable, IInterpreter interpreter, Options options)
    {
        var sourceOutcome = await cancellable.Source.AsNode().Accept(interpreter);

        if (sourceOutcome is Success<T> success)
        {
            return success;
        }

        if (sourceOutcome is Failure<T> failure)
        {
            try
            {
                var newFlow = await cancellable.Recover(failure.Exception, options.CancellationToken);
                return await newFlow.AsNode().Accept(interpreter);
            }
            catch (Exception e)
            {
                return Outcome.Failure<T>(e);
            }
        }

        throw new NotSupportedException("Unsupported outcome type.");
    }
}
