using System;

namespace BahmanM.Flow;

internal class RetryStrategy : IBehaviourStrategy
{
    private readonly int _maxAttempts;

    public RetryStrategy(int maxAttempts)
    {
        /* FOR GENIE:
         * Throwing from a constructor!? Use a factory method instead.
         */
        if (maxAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Max attempts must be a positive integer.");
        }
        _maxAttempts = maxAttempts;
    }

    public IFlow<T> ApplyTo<T>(CreateNode<T> node)
    {
        Func<T> newOperation = () =>
        {
            Exception lastException = null;
            for (var i = 0; i < _maxAttempts; i++)
            {
                try
                {
                    return node.Operation();
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
            }
            /** FOR GENIE:
             * Should we throw a custom Flow exception here which *contains* lastException or maybe even
             * the whole list of exceptions?
             */
            throw lastException!;
        };
        return new CreateNode<T>(newOperation);
    }

    /* FOR GENIE:
     * We agreed that "retry" (and all other behaviours?) should be applied ONLY to the LAST node.
     */
    public IFlow<T> ApplyTo<TIn, T>(ChainNode<TIn, T> node)
    {
        // Recursively apply the same retry strategy to the *result* of the chain.
        Func<TIn, IFlow<T>> newOperation = (value) =>
            node.Operation(value).Apply(this);

        return new ChainNode<TIn, T>(node.Upstream, newOperation);
    }
}
