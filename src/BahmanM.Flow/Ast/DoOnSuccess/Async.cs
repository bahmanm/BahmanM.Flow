namespace BahmanM.Flow.Ast.DoOnSuccess;

internal sealed record Async<T>(IFlow<T> Upstream, Operations.DoOnSuccess.Async<T> AsyncAction) : INode<T>
{
    public Task<Outcome<T>> Accept(IInterpreter<T> interpreter) => interpreter.Interpret(this);
    public IFlow<T> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
