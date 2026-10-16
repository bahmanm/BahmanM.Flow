using BahmanM.Flow.Behaviour;

namespace BahmanM.Flow.Ast.Resource;

internal sealed record WithResourceCancellableAsync<TResource, T>(
    Func<CancellationToken, Task<TResource>> AcquireAsync,
    Func<TResource, IFlow<T>> Use) : INode<T>
    where TResource : IAsyncDisposable
{
    public Task<Outcome<T>> Accept(IInterpreter interpreter) => interpreter.Interpret(this);
    public IFlow<T> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}

