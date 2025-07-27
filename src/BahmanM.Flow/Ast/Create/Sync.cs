namespace BahmanM.Flow.Ast.Create;

internal sealed record Sync<T>(Func<T> Operation) : INode<T>
{
    public Task<Outcome<T>> Accept(IInterpreter interpreter) => interpreter.Interpret(this);
    public IFlow<T> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
