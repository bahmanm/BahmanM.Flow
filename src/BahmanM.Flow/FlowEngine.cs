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

    public static Task<Outcome<T>> ExecuteAsync<T>(IFlow<T> flow, FlowExecutionOptions options)
    {
        var node = (IFlowNode<T>)flow;
        return node.ExecuteWith(new FlowEngine(options));
    }

    #endregion

    #region Constructors

    private readonly FlowExecutionOptions _options;

    private CancellationToken CancellationToken => _options.CancellationToken;

    private FlowEngine()
    {
        _options = new FlowExecutionOptions();
    }

    private FlowEngine(FlowExecutionOptions options)
    {
        _options = options;
    }

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

        return upstreamOutcome switch
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

    internal async Task<Outcome<T>> Execute<T>(CancellableAsyncDoOnSuccessNode<T> node)
    {
        var upstreamOutcome = await ((IFlowNode<T>)node.Upstream).ExecuteWith(this);

        if (upstreamOutcome is Success<T> success)
        {
            try
            {
                await node.AsyncAction(success.Value, CancellationToken);
                return upstreamOutcome;
            }
            catch (Exception ex)
            {
                return Outcome.Failure<T>(ex);
            }
        }

        return upstreamOutcome;
    }

    internal async Task<Outcome<T>> Execute<T>(DoOnFailureNode<T> node)
    {
        var upstreamOutcome = await ((IFlowNode<T>)node.Upstream).ExecuteWith(this);

        if (upstreamOutcome is Failure<T> failure)
        {
            try { node.Action(failure.Exception); }
            catch { /* Ignore */ }
        }

        return upstreamOutcome;
    }

    internal async Task<Outcome<T>> Execute<T>(AsyncDoOnFailureNode<T> node)
    {
        var upstreamOutcome = await ((IFlowNode<T>)node.Upstream).ExecuteWith(this);

        if (upstreamOutcome is Failure<T> failure)
        {
            try { await node.AsyncAction(failure.Exception); }
            catch { /* Ignore */ }
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

    internal async Task<Outcome<TOut>> Execute<TIn, TOut>(ChainNode<TIn, TOut> node)
    {
        var upstreamOutcome = await ((IFlowNode<TIn>)node.Upstream).ExecuteWith(this);

        if (upstreamOutcome is not Success<TIn> success)
        {
            return Outcome.Failure<TOut>(((Failure<TIn>)upstreamOutcome).Exception);
        }

        try
        {
            var nextFlow = (IFlowNode<TOut>)node.Operation(success.Value);
            return await nextFlow.ExecuteWith(this);
        }
        catch (Exception ex)
        {
            return Outcome.Failure<TOut>(ex);
        }
    }

    internal async Task<Outcome<TOut>> Execute<TIn, TOut>(AsyncChainNode<TIn, TOut> node)
    {
        var upstreamOutcome = await ((IFlowNode<TIn>)node.Upstream).ExecuteWith(this);

        if (upstreamOutcome is not Success<TIn> success)
        {
            return Outcome.Failure<TOut>(((Failure<TIn>)upstreamOutcome).Exception);
        }

        try
        {
            var nextFlow = (IFlowNode<TOut>)await node.Operation(success.Value);
            return await nextFlow.ExecuteWith(this);
        }
        catch (Exception ex)
        {
            return Outcome.Failure<TOut>(ex);
        }
    }

    internal async Task<Outcome<T[]>> Execute<T>(AllNode<T> node)
    {
        // This implementation waits for all flows to complete. If any have failed, it
        // aggregates all their exceptions. This provides the most comprehensive diagnostic
        // information to the caller, rather than failing fast on the first exception.
        var outcomes = await Task.WhenAll(node.Flows.Select(f => ((IFlowNode<T>)f).ExecuteWith(this)));

        var exceptions = outcomes.OfType<Failure<T>>().Select(f => f.Exception).ToList();

        return exceptions is not []
            ? Outcome.Failure<T[]>(new AggregateException(exceptions))
            : Outcome.Success(outcomes.OfType<Success<T>>().Select(s => s.Value).ToArray());
    }

    internal async Task<Outcome<T>> Execute<T>(AnyNode<T> node)
    {
        return await ExecuteAnyRecursively(
            node.Flows.Select(f => ((IFlowNode<T>)f).ExecuteWith(this)).ToList(),
            []);
    }

    #endregion

    #region Private Helpers

    private async Task<Outcome<T>> TryFindFirstSuccessfulFlow<T>(List<Task<Outcome<T>>> remainingTasks, List<Exception> accumulatedExceptions)
    {
        if (remainingTasks is [])
        {
            return Outcome.Failure<T>(new AggregateException(accumulatedExceptions));
        }

        var completedTask = await Task.WhenAny(remainingTasks);
        remainingTasks.Remove(completedTask);

        var outcome = await completedTask;

        if (outcome is Success<T> success)
        {
            // TODO: Cancel the remaining tasks. This requires passing a CancellationTokenSource through the engine.
            // For now, we let them run to completion but ignore their results.
            return success;
        }

        accumulatedExceptions.Add(((Failure<T>)outcome).Exception);
        return await TryFindFirstSuccessfulFlow(remainingTasks, accumulatedExceptions);
    }

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