using BahmanM.Flow.Behaviour;
using BahmanM.Flow.Shared;

namespace BahmanM.Flow.Ast;

internal interface IBehaviourStrategyFactory
{
    IBehaviourStrategy CreateRetry(int maxAttempts, Type[] nonRetryableExceptions);
    IBehaviourStrategy CreateTimeout(TimeSpan duration);
    INode<T> CreateRecoverSync<T>(IFlow<T> source, Operations.Recover.Sync<T> recover);
    INode<T> CreateRecoverAsync<T>(IFlow<T> source, Operations.Recover.Async<T> recover);
    INode<T> CreateRecoverCancellableAsync<T>(IFlow<T> source, Operations.Recover.CancellableAsync<T> recover);
}
