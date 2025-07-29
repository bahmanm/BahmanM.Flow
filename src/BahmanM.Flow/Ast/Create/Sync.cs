namespace BahmanM.Flow.Ast.Create;

internal sealed record Sync<T>(Operations.Create.Sync<T> Operation) : Execution.IExecutionNode<T>
{
    public Task<Outcome<T>> Accept(Execution.IInterpreter<T> interpreter) => interpreter.Interpret(this);
    public IFlow<T> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
}
