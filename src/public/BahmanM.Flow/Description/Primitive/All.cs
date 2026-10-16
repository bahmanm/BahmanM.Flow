namespace BahmanM.Flow.Description.Primitive;

internal sealed record All<T> : Node<T[]>
{
    internal All(IEnumerable<IDescription<T>> flows) =>
        Flows = Array.AsReadOnly(flows.ToArray());

    internal IReadOnlyList<IDescription<T>> Flows { get; }

    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);

    public override Ast.INode<T[]> Lower() =>
        new Ast.Primitive.All<T>(Flows.Select(flow => (IFlow<T>)flow.Lower()).ToArray());
}
