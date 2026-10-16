using BahmanM.Flow.Ast;
using BahmanM.Flow.Behaviour;
using BahmanM.Flow.Shared;

namespace BahmanM.Flow.Execution;

internal sealed class BehaviourStrategyFactory : IBehaviourStrategyFactory
{
    internal static BehaviourStrategyFactory Instance { get; } = new();

    public IBehaviourStrategy CreateRetry(int maxAttempts, Type[] nonRetryableExceptions) =>
        new RetryStrategy(maxAttempts, nonRetryableExceptions);

    public IBehaviourStrategy CreateTimeout(TimeSpan duration) => new TimeoutStrategy(duration);

    public INode<T> CreateRecoverSync<T>(IFlow<T> source, Operations.Recover.Sync<T> recover) =>
        new Ast.Recover.Internal.Sync<T>(source, recover);

    public INode<T> CreateRecoverAsync<T>(IFlow<T> source, Operations.Recover.Async<T> recover) =>
        new Ast.Recover.Internal.Async<T>(source, recover);

    public INode<T> CreateRecoverCancellableAsync<T>(IFlow<T> source, Operations.Recover.CancellableAsync<T> recover) =>
        new Ast.Recover.Internal.CancellableAsync<T>(source, recover);
}
