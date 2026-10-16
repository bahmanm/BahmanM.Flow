using BahmanM.Flow.Support;

namespace BahmanM.Flow.Description.Behaviour.Custom;

internal sealed record Custom<T> : Node<T>
{
    internal Custom(IDescription<T> source, BahmanM.Flow.Behaviour.IBehaviour behaviour)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Behaviour = behaviour ?? throw new ArgumentNullException(nameof(behaviour));
    }

    internal IDescription<T> Source { get; }
    internal BahmanM.Flow.Behaviour.IBehaviour Behaviour { get; }

    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);

    public override Ast.INode<T> Lower() => Behaviour.Apply(Source.Lower()).AsNode();
}
