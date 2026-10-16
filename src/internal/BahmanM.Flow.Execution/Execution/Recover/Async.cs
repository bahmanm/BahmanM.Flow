using BahmanM.Flow.Ast;
using BahmanM.Flow.Support;
using BahmanM.Flow.Ast.Recover.Internal;

namespace BahmanM.Flow.Execution.Recover;

internal static class Async
{
    internal static async Task<Outcome<T>> Execute<T>(Async<T> async, IInterpreter interpreter, Options options)
    {
        var sourceOutcome = await async.Source.AsNode(BehaviourStrategyFactory.Instance).Accept(interpreter);

        if (sourceOutcome is Success<T> success)
        {
            return success;
        }

        if (sourceOutcome is Failure<T> failure)
        {
            try
            {
                var newFlow = await async.Recover(failure.Exception);
                return await newFlow.AsNode(BehaviourStrategyFactory.Instance).Accept(interpreter);
            }
            catch (Exception e)
            {
                return Outcome.Failure<T>(e);
            }
        }

        throw new NotSupportedException("Unsupported outcome type.");
    }
}
