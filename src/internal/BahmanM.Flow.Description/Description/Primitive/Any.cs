namespace BahmanM.Flow.Description.Primitive;

internal sealed record Any<T> : Node<T>
{
    internal Any(IEnumerable<IDescription<T>> flows) =>
        Flows = Array.AsReadOnly(flows.ToArray());

    internal IReadOnlyList<IDescription<T>> Flows { get; }

    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);

    public override Ast.INode<T> Lower(Ast.IBehaviourStrategyFactory factory) =>
        new Ast.Primitive.Any<T>([.. Flows.Select(flow => (IFlow<T>)flow.Lower(factory))]);
}
