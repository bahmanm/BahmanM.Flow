namespace BahmanM.Flow.Ast.Select;

internal sealed record CancellableAsync<TIn, TValue>(IFlow<TIn> Upstream, Operations.Select.CancellableAsync<TIn, TValue> Operation) : INode<TValue>
{
    public Task<Outcome<TValue>> Accept(Ast.IInterpreter<Task<Outcome<TValue>>> interpreter) => interpreter.Interpret(this);
    public IFlow<TValue> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
