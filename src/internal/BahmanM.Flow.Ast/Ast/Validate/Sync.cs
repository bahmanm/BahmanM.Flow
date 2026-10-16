using BahmanM.Flow.Behaviour;

namespace BahmanM.Flow.Ast.Validate;

internal sealed record Sync<T>(IFlow<T> Upstream, Func<T, bool> Predicate, Func<T, Exception> ExceptionFactory) : INode<T>
{
    public Task<Outcome<T>> Accept(IInterpreter interpreter) => interpreter.Interpret(this);
    public IFlow<T> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}

