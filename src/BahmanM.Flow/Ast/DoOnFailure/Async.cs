namespace BahmanM.Flow.Ast.DoOnFailure;

internal sealed record Async<T>(IFlow<T> Upstream, Operations.DoOnFailure.Async AsyncAction) : INode<T>
{
    public Task<Outcome<T>> Accept(IInterpreter<T> interpreter) => interpreter.Interpret(this);
    public IFlow<T> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
