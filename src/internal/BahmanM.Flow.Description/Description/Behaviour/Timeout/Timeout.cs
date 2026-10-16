using BahmanM.Flow.Support;

namespace BahmanM.Flow.Description.Behaviour.Timeout;

internal sealed record Timeout<T> : Node<T>
{
    internal Timeout(IDescription<T> source, TimeSpan duration)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Duration = duration;
    }

    internal IDescription<T> Source { get; }
    internal TimeSpan Duration { get; }

    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);

    public override Ast.INode<T> Lower(Ast.IBehaviourStrategyFactory factory)
    {
        var source = Source.Lower(factory);
        var strategy = factory.CreateTimeout(Duration);
        return source.Apply(strategy).AsNode(factory);
    }
}
