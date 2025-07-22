using System;

namespace BahmanM.Flow;

internal class RetryStrategy : IBehaviourStrategy
{
    private readonly int _maxAttempts;

    public RetryStrategy(int maxAttempts)
    {
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
            Exception lastException = null!;
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
            throw lastException!;
        };
        return new CreateNode<T>(newOperation);
    }

    public IFlow<T> ApplyTo<TIn, T>(ChainNode<TIn, T> node)
    {
        Func<TIn, IFlow<T>> newOperation = (value) =>
            ((IFlowNode<T>)node.Operation(value)).Apply(this);

        return node with { Operation = newOperation };
    }
}
