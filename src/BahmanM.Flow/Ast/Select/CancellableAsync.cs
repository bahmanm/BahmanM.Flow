namespace BahmanM.Flow.Ast.Select;

internal sealed record CancellableAsync<TIn, TValue>(IFlow<TIn> Upstream, Operations.Select.CancellableAsync<TIn, TValue> Operation) : INode<TValue>
{
    public Task<Outcome<TValue>> Accept(Ast.IInterpreter<TValue, Task<Outcome<TValue>>> interpreter) => interpreter.Interpret(this);
    public IFlow<TValue> Apply(IBehaviourStrategy<TValue> strategy) => strategy.ApplyTo(this);
}
