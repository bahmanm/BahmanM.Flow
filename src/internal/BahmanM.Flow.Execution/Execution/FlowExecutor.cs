using BahmanM.Flow.Support;

namespace BahmanM.Flow.Execution;

internal static class FlowExecutor
{
    internal static Task<Outcome<T>> ExecuteAsync<T>(IFlow<T> flow, Options options) =>
        ExecuteAsync(flow, options, BehaviourStrategyFactory.Instance);

    internal static Task<Outcome<T>> ExecuteAsync<T>(Description.IDescription<T> description, Options options) =>
        ExecuteAsync(description, options, BehaviourStrategyFactory.Instance);

    internal static Task<Outcome<T>> ExecuteAsync<T>(
        IFlow<T> flow,
        Options options,
        Ast.IBehaviourStrategyFactory behaviourStrategyFactory) =>
        flow.AsNode(behaviourStrategyFactory).Accept(new Interpreter(options));

    internal static Task<Outcome<T>> ExecuteAsync<T>(
        Description.IDescription<T> description,
        Options options,
        Ast.IBehaviourStrategyFactory behaviourStrategyFactory) =>
        description.Lower(behaviourStrategyFactory).Accept(new Interpreter(options));
}
