namespace BahmanM.Flow;

public class FlowEngine
{
    #region Public API
    
    public static Task<Outcome<T>> ExecuteAsync<T>(IFlow<T> flow)
    {
        // A safe, controlled cast as Flow owns all (sub)types.
        var executableFlow = (IVisitableFlow<T>)flow;
        return executableFlow.ExecuteWith(new FlowEngine());
    }

    #endregion

    #region Constructors
    
    private FlowEngine() { }

    #endregion

    #region Visitor Methods

    internal Task<Outcome<T>> Execute<T>(SucceededFlow<T> flow) =>
        Task.FromResult(Success(flow.Value));

    internal Task<Outcome<T>> Execute<T>(FailedFlow<T> flow) =>
        Task.FromResult(Failure<T>(flow.Exception));

    internal Task<Outcome<T>> Execute<T>(CreateFlow<T> flow) =>
        TryOperation(flow.Operation);

    internal Task<Outcome<T>> Execute<T>(AsyncCreateFlow<T> flow) =>
        TryOperation(flow.Operation);

    internal async Task<Outcome<T>> Execute<T>(DoOnSuccessFlow<T> flow)
    {
        var upstreamOutcome = await ((IVisitableFlow<T>)flow.Upstream).ExecuteWith(this);

        if (upstreamOutcome is Success<T> success)
        {
            try
            {
                flow.Action(success.Value);
                return upstreamOutcome;
            }
            catch (Exception ex)
            {
                return Failure<T>(ex);
            }
        }

        return upstreamOutcome;
    }

    internal async Task<Outcome<T>> Execute<T>(AsyncDoOnSuccessFlow<T> flow)
    {
        var upstreamOutcome = await ((IVisitableFlow<T>)flow.Upstream).ExecuteWith(this);

        if (upstreamOutcome is Success<T> success)
        {
            try
            {
                await flow.AsyncAction(success.Value);
                return upstreamOutcome;
            }
            catch (Exception ex)
            {
                return Failure<T>(ex);
            }
        }

        return upstreamOutcome;
    }

    internal async Task<Outcome<TOut>> Execute<TIn, TOut>(SelectFlow<TIn, TOut> flow)
    {
        var upstreamOutcome = await ((IVisitableFlow<TIn>)flow.Upstream).ExecuteWith(this);

        return upstreamOutcome switch
        {
            Success<TIn> s => await TryOperation(() => flow.Operation(s.Value)),
            Failure<TIn> f => Failure<TOut>(f.Exception),
            _ => throw new NotSupportedException($"Unsupported outcome type: {upstreamOutcome.GetType().Name}")
        };
    }

    internal async Task<Outcome<TOut>> Execute<TIn, TOut>(AsyncSelectFlow<TIn, TOut> flow)
    {
        var upstreamOutcome = await ((IVisitableFlow<TIn>)flow.Upstream).ExecuteWith(this);

        return upstreamOutcome switch
        {
            Success<TIn> s => await TryOperation(() => flow.Operation(s.Value)),
            Failure<TIn> f => Failure<TOut>(f.Exception),
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
