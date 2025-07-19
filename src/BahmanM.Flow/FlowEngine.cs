namespace BahmanM.Flow;

public static class FlowEngine
{
    public static Outcome<T> Execute<T>(IFlow<T> flow)
    {
        return flow switch
        {
            SucceededFlow<T> successFlow => Outcome.Success(successFlow.Value),
            FailedFlow<T> failedFlow => Outcome.Failure<T>(failedFlow.Exception),
            CreateFlow<T> createFlow => TryOperation(createFlow.Operation),
            _ => throw new NotSupportedException($"Unsupported flow type: {flow.GetType().Name}")
        };
    }

    private static Outcome<T> TryOperation<T>(Func<T> operation)
    {
        try
        {
            return Outcome.Success(operation());
        }
        catch (Exception ex)
        {
            return Outcome.Failure<T>(ex);
        }
    }
}
