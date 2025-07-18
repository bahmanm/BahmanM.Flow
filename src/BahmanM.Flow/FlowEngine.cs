namespace BahmanM.Flow;

public static class FlowEngine
{
    public static Outcome<T> Execute<T>(IFlow<T> flow)
    {
        return flow switch
        {
            Flow.SucceededFlow<T> successFlow => new Success<T>(successFlow.Value),
            _ => throw new NotSupportedException($"Unsupported flow type: {flow.GetType().Name}")
        };
    }
}
