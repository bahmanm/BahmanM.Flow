namespace BahmanM.Flow.Ast.DoOnFailure;

internal sealed record Sync<TValue>(IFlow<TValue> Upstream, Operations.DoOnFailure.Sync Action) : INode<TValue>
{
    public Task<Outcome<TValue>> Accept(Ast.IInterpreter<Task<Outcome<TValue>>> interpreter) => interpreter.Interpret(this);
    public IFlow<TValue> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
