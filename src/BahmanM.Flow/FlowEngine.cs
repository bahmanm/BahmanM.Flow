namespace BahmanM.Flow;

public static class FlowEngine
{
    public static async Task<Outcome<T>> ExecuteAsync<T>(IFlow<T> flow)
    {
        return flow switch
        {
            SucceededFlow<T> successFlow => Outcome.Success(successFlow.Value),
            FailedFlow<T> failedFlow => Outcome.Failure<T>(failedFlow.Exception),
            CreateFlow<T> createFlow => await TryOperation(createFlow.Operation),
            AsyncCreateFlow<T> asyncCreateFlow => await TryOperation(asyncCreateFlow.Operation),
            _ => throw new NotSupportedException($"Unsupported flow type: {flow.GetType().Name}")
        };
    }

    private static Task<Outcome<T>> TryOperation<T>(Func<T> operation)
    {
        try
        {
            return Task.FromResult(Outcome.Success(operation()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Outcome.Failure<T>(ex));
        }
    }

    private static async Task<Outcome<T>> TryOperation<T>(Func<Task<T>> operation)
    {
        try
        {
            return Outcome.Success(await operation());
        }
        catch (Exception ex)
        {
            return Outcome.Failure<T>(ex);
        }
    }
}