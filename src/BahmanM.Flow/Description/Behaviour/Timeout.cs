using BahmanM.Flow.Behaviour;
using BahmanM.Flow.Support;

namespace BahmanM.Flow.Description.Behaviour;

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

    public override Ast.INode<T> Lower()
    {
        var source = Source.Lower();
        var strategy = new TimeoutStrategy(Duration);
        return source.Apply(strategy).AsNode();
    }
}
