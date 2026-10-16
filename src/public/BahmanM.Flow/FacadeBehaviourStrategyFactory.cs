using BahmanM.Flow.Ast;
using BahmanM.Flow.Behaviour;
using BahmanM.Flow.Shared;

namespace BahmanM.Flow;

internal sealed class FacadeBehaviourStrategyFactory : IBehaviourStrategyFactory
{
    internal static FacadeBehaviourStrategyFactory Instance { get; } = new();

    public IBehaviourStrategy CreateRetry(int maxAttempts, Type[] nonRetryableExceptions) =>
        new Behaviour.RetryStrategy(maxAttempts, nonRetryableExceptions);

    public IBehaviourStrategy CreateTimeout(TimeSpan duration) => new Behaviour.TimeoutStrategy(duration);

    public INode<T> CreateRecoverSync<T>(IFlow<T> source, Operations.Recover.Sync<T> recover) =>
        new Ast.Recover.Sync<T>(source, error => recover(error));

    public INode<T> CreateRecoverAsync<T>(IFlow<T> source, Operations.Recover.Async<T> recover) =>
        new Ast.Recover.Async<T>(source, error => recover(error));

    public INode<T> CreateRecoverCancellableAsync<T>(IFlow<T> source, Operations.Recover.CancellableAsync<T> recover) =>
        new Ast.Recover.CancellableAsync<T>(source, (error, cancellationToken) => recover(error, cancellationToken));
}
