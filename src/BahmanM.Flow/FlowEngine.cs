namespace BahmanM.Flow;

public static class FlowEngine
{
    public static Outcome<T> Execute<T>(IFlow<T> flow)
    {
        return flow switch
        {
            Flow.SucceededFlow<T> successFlow => new Success<T>(successFlow.Value),
            Flow.FailedFlow<T> failedFlow => new Failure<T>(failedFlow.Exception),
            Flow.CreateFlow<T> createFlow => TryOperation(createFlow.Operation),
            _ => throw new NotSupportedException($"Unsupported flow type: {flow.GetType().Name}")
        };
    }

    private static Outcome<T> TryOperation<T>(Func<T> operation)
    {
        try
        {
            return new Success<T>(operation());
        }
        catch (Exception ex)
        {
            return new Failure<T>(ex);
        }
    }
}
