namespace BahmanM.Flow;

public class FlowEngine
{
    #region Public API

    public static Task<Outcome<T>> ExecuteAsync<T>(IFlow<T> flow)
    {
        var node = (IFlowNode<T>)flow;
        return node.ExecuteWith(new FlowEngine());
    }

    #endregion

    #region Constructors

    private FlowEngine() { }

    #endregion

    #region Visitor Methods

    internal Task<Outcome<T>> Execute<T>(SucceededNode<T> node) =>
        Task.FromResult(Outcome.Success(node.Value));

    internal Task<Outcome<T>> Execute<T>(FailedNode<T> node) =>
        Task.FromResult(Outcome.Failure<T>(node.Exception));

    internal Task<Outcome<T>> Execute<T>(CreateNode<T> node) =>
        TryOperation(node.Operation);

    internal Task<Outcome<T>> Execute<T>(AsyncCreateNode<T> node) =>
        TryOperation(node.Operation);

    internal async Task<Outcome<T>> Execute<T>(DoOnSuccessNode<T> node)
    {
        var upstreamOutcome = await ((IFlowNode<T>)node.Upstream).ExecuteWith(this);

        if (upstreamOutcome is Success<T> success)
        {
            try
            {
                node.Action(success.Value);
                return upstreamOutcome;
            }
            catch (Exception ex)
            {
                return Outcome.Failure<T>(ex);
            }
        }

        return upstreamOutcome;
    }

    internal async Task<Outcome<T>> Execute<T>(AsyncDoOnSuccessNode<T> node)
    {
        var upstreamOutcome = await ((IFlowNode<T>)node.Upstream).ExecuteWith(this);

        if (upstreamOutcome is Success<T> success)
        {
            try
            {
                await node.AsyncAction(success.Value);
                return upstreamOutcome;
            }
            catch (Exception ex)
            {
                return Outcome.Failure<T>(ex);
            }
        }

        return upstreamOutcome;
    }

    internal async Task<Outcome<TOut>> Execute<TIn, TOut>(SelectNode<TIn, TOut> node)
    {
        var upstreamOutcome = await ((IFlowNode<TIn>)node.Upstream).ExecuteWith(this);

        return upstreamOutcome switch
        {
            Success<TIn> s => await TryOperation(() => node.Operation(s.Value)),
            Failure<TIn> f => Outcome.Failure<TOut>(f.Exception),
            _ => throw new NotSupportedException($"Unsupported outcome type: {upstreamOutcome.GetType().Name}")
        };
    }

    internal async Task<Outcome<TOut>> Execute<TIn, TOut>(AsyncSelectNode<TIn, TOut> node)
    {
        var upstreamOutcome = await ((IFlowNode<TIn>)node.Upstream).ExecuteWith(this);

        return upstreamOutcome switch
        {
            Success<TIn> s => await TryOperation(async () => await node.Operation(s.Value)),
            Failure<TIn> f => Outcome.Failure<TOut>(f.Exception),
            _ => throw new NotSupportedException($"Unsupported outcome type: {upstreamOutcome.GetType().Name}")
        };
    }

    #endregion

    #region Private Helpers

    private static Task<Outcome<T>> TryOperation<T>(Func<T> operation)
    {
        try
        {
            return Task.FromResult(Outcome.Success(operation()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Outcome.Failure<T>(ex));
        }
    }

    private static async Task<Outcome<T>> TryOperation<T>(Func<Task<T>> operation)
    {
        try
        {
            return Outcome.Success(await operation());
        }
        catch (Exception ex)
        {
            return Outcome.Failure<T>(ex);
        }
    }

    #endregion
}
