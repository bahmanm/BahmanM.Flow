namespace BahmanM.Flow.Ast.Primitive
{
    internal sealed record Any<T>(IReadOnlyList<IFlow<T>> Flows) : INode<T>
    {
        public Task<Outcome<T>> ExecuteWith(FlowEngine engine) => engine.Execute(this);
        public IFlow<T> Apply(IBehaviourStrategy strategy) => strategy.ApplyTo(this);
    }

}
