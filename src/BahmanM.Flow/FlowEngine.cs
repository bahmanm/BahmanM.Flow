using BahmanM.Flow.Execution;

namespace BahmanM.Flow;

public static class FlowEngine
{
    public static Task<Outcome<T>> ExecuteAsync<T>(IFlow<T> flow) =>
        ExecuteAsync(flow, new Execution.Options(CancellationToken.None));

    public static Task<Outcome<T>> ExecuteAsync<T>(IFlow<T> flow, Execution.Options options) =>
        flow.AsNode().Accept(new Execution.Interpreter(options));
}
