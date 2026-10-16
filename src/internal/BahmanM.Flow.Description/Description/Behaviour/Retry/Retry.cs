using System.Collections.Immutable;
using BahmanM.Flow.Support;

namespace BahmanM.Flow.Description.Behaviour.Retry;

internal sealed record Retry<T> : Node<T>
{
    internal Retry(
        IDescription<T> source,
        int maxAttempts,
        IEnumerable<Type>? nonRetryableExceptions)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        MaxAttempts = maxAttempts > 0
            ? maxAttempts
            : throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Max attempts must be a positive integer.");
        NonRetryableExceptions = nonRetryableExceptions?.ToImmutableArray()
            ?? throw new ArgumentNullException(nameof(nonRetryableExceptions));
    }

    internal IDescription<T> Source { get; }
    internal int MaxAttempts { get; }
    internal ImmutableArray<Type> NonRetryableExceptions { get; }

    public override TResult Accept<TResult>(IDescriptionVisitor<TResult> visitor) => visitor.Visit(this);

    public override Ast.INode<T> Lower(Ast.IBehaviourStrategyFactory factory)
    {
        var source = Source.Lower(factory);
        var strategy = factory.CreateRetry(MaxAttempts, [.. NonRetryableExceptions]);
        return source.Apply(strategy).AsNode(factory);
    }
}
