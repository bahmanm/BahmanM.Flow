using BahmanM.Flow.Behaviour;

namespace BahmanM.Flow.Ast.Resource;

internal sealed record WithResource<TResource, T>(Func<TResource> Acquire, Func<TResource, IFlow<T>> Use) : INode<T>
    where TResource : IDisposable
{
    public Task<Outcome<T>> Accept(IInterpreter interpreter) => interpreter.Interpret(this);
    public IFlow<T> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}

