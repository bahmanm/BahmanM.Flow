using BahmanM.Flow.Support;

namespace BahmanM.Flow.Description.Behaviour.Custom;

internal sealed record Custom<T> : Node<T>
{
    internal Custom(IDescription<T> source, BahmanM.Flow.Behaviour.IBehaviour behaviour)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Behaviour = behaviour ?? throw new ArgumentNullException(nameof(behaviour));
    }

    internal Custom(
        IDescription<T> source,
        BahmanM.Flow.Behaviour.IBehaviour behaviour,
        IDescription<T> cachedResultDescription)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Behaviour = behaviour ?? throw new ArgumentNullException(nameof(behaviour));
        CachedResultDescription = cachedResultDescription
            ?? throw new ArgumentNullException(nameof(cachedResultDescription));
    }

    internal IDescription<T> Source { get; }
    internal BahmanM.Flow.Behaviour.IBehaviour Behaviour { get; }
    internal IDescription<T>? CachedResultDescription { get; }

    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);

    public override Ast.INode<T> Lower() =>
        CachedResultDescription?.Lower() ?? Behaviour.Apply(Source.Lower()).AsNode();
}
