namespace BahmanM.Flow;

public class FlowEngine
{
    #region Public API
    
    public static Task<Outcome<T>> ExecuteAsync<T>(IFlow<T> flow)
    {
        var node = (Ast.INode<T>)flow;
        return node.ExecuteWith(new FlowEngine());
    }

    public static Task<Outcome<T>> ExecuteAsync<T>(IFlow<T> flow, FlowExecutionOptions options)
    {
        var node = (Ast.INode<T>)flow;
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

    internal Task<Outcome<T>> Execute<T>(Ast.Pure.Succeed<T> node) =>
        Task.FromResult(Outcome.Success(node.Value));

    internal Task<Outcome<T>> Execute<T>(Ast.Pure.Fail<T> node) =>
        Task.FromResult(Outcome.Failure<T>(node.Exception));

    internal Task<Outcome<T>> Execute<T>(Ast.Create.Sync<T> node) =>
        TryOperation.TrySync<T>(() => node.Operation());

    internal Task<Outcome<T>> Execute<T>(Ast.Create.Async<T> node) =>
        TryOperation.TryAsync<T>(() => node.Operation());

    internal Task<Outcome<T>> Execute<T>(Ast.Create.CancellableAsync<T> node) =>
        TryOperation.TryCancellableAsync<T>((cancellationToken) => node.Operation(cancellationToken),
            CancellationToken);

    internal async Task<Outcome<T>> Execute<T>(Ast.DoOnSuccess.Sync<T> node)
    {
        var upstreamOutcome = await ((Ast.INode<T>)node.Upstream).ExecuteWith(this);

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

    internal async Task<Outcome<T>> Execute<T>(Ast.DoOnSuccess.Async<T> node)
    {
        var upstreamOutcome = await ((Ast.INode<T>)node.Upstream).ExecuteWith(this);

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

    internal async Task<Outcome<T>> Execute<T>(Ast.DoOnSuccess.CancellableAsync<T> node)
    {
        var upstreamOutcome = await ((Ast.INode<T>)node.Upstream).ExecuteWith(this);

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

    internal async Task<Outcome<T>> Execute<T>(Ast.DoOnFailure.Sync<T> node)
    {
        var upstreamOutcome = await ((Ast.INode<T>)node.Upstream).ExecuteWith(this);

        if (upstreamOutcome is Failure<T> failure)
        {
            try
            {
                node.Action(failure.Exception);
            }
            catch
            {
                /* Ignore */
            }
        }

        return upstreamOutcome;
    }

    internal async Task<Outcome<T>> Execute<T>(Ast.DoOnFailure.Async<T> node)
    {
        var upstreamOutcome = await ((Ast.INode<T>)node.Upstream).ExecuteWith(this);

        if (upstreamOutcome is Failure<T> failure)
        {
            try
            {
                await node.AsyncAction(failure.Exception);
            }
            catch
            {
                /* Ignore */
            }
        }

        return upstreamOutcome;
    }

    internal async Task<Outcome<T>> Execute<T>(Ast.DoOnFailure.CancellableAsync<T> node)
    {
        var upstreamOutcome = await ((Ast.INode<T>)node.Upstream).ExecuteWith(this);

        if (upstreamOutcome is Failure<T> failure)
        {
            try
            {
                await node.AsyncAction(failure.Exception, CancellationToken);
            }
            catch
            {
                /* Ignore */
            }
        }

        return upstreamOutcome;
    }

    internal async Task<Outcome<TOut>> Execute<TIn, TOut>(Ast.Select.Sync<TIn, TOut> node)
    {
        var upstreamOutcome = await ((Ast.INode<TIn>)node.Upstream).ExecuteWith(this);

        return upstreamOutcome switch
        {
            Success<TIn> s => await TryOperation.TrySync(() => node.Operation(s.Value)),
            Failure<TIn> f => Outcome.Failure<TOut>(f.Exception),
            _ => throw new NotSupportedException($"Unsupported outcome type: {upstreamOutcome.GetType().Name}")
        };
    }

    internal async Task<Outcome<TOut>> Execute<TIn, TOut>(Ast.Select.Async<TIn, TOut> node)
    {
        var upstreamOutcome = await ((Ast.INode<TIn>)node.Upstream).ExecuteWith(this);

        return upstreamOutcome switch
        {
            Success<TIn> s => await TryOperation.TryAsync(() => node.Operation(s.Value)),
            Failure<TIn> f => Outcome.Failure<TOut>(f.Exception),
            _ => throw new NotSupportedException($"Unsupported outcome type: {upstreamOutcome.GetType().Name}")
        };
    }

    internal async Task<Outcome<TOut>> Execute<TIn, TOut>(Ast.Select.CancellableAsync<TIn, TOut> node)
    {
        var upstreamOutcome = await ((Ast.INode<TIn>)node.Upstream).ExecuteWith(this);

        return upstreamOutcome switch
        {
            Success<TIn> s => await TryOperation.TryCancellableAsync(ct => node.Operation(s.Value, ct), CancellationToken),
            Failure<TIn> f => Outcome.Failure<TOut>(f.Exception),
            _ => throw new NotSupportedException($"Unsupported outcome type: {upstreamOutcome.GetType().Name}")
        };
    }

    internal async Task<Outcome<TOut>> Execute<TIn, TOut>(Ast.Chain.Sync<TIn, TOut> node)
    {
        var upstreamOutcome = await ((Ast.INode<TIn>)node.Upstream).ExecuteWith(this);

        if (upstreamOutcome is not Success<TIn> success)
        {
            return Outcome.Failure<TOut>(((Failure<TIn>)upstreamOutcome).Exception);
        }

        try
        {
            var nextFlow = (Ast.INode<TOut>)node.Operation(success.Value);
            return await nextFlow.ExecuteWith(this);
        }
        catch (Exception ex)
        {
            return Outcome.Failure<TOut>(ex);
        }
    }

    internal async Task<Outcome<TOut>> Execute<TIn, TOut>(Ast.Chain.Async<TIn, TOut> node)
    {
        var upstreamOutcome = await ((Ast.INode<TIn>)node.Upstream).ExecuteWith(this);

        if (upstreamOutcome is not Success<TIn> success)
        {
            return Outcome.Failure<TOut>(((Failure<TIn>)upstreamOutcome).Exception);
        }

        try
        {
            var nextFlow = (Ast.INode<TOut>)await node.Operation(success.Value);
            return await nextFlow.ExecuteWith(this);
        }
        catch (Exception ex)
        {
            return Outcome.Failure<TOut>(ex);
        }
    }

    internal async Task<Outcome<TOut>> Execute<TIn, TOut>(Ast.Chain.CancellableAsync<TIn, TOut> node)
    {
        var upstreamOutcome = await ((Ast.INode<TIn>)node.Upstream).ExecuteWith(this);

        if (upstreamOutcome is not Success<TIn> success)
        {
            return Outcome.Failure<TOut>(((Failure<TIn>)upstreamOutcome).Exception);
        }

        try
        {
            if (CancellationToken.IsCancellationRequested)
            {
                return Outcome.Failure<TOut>(new TaskCanceledException());
            }

            var nextFlow = (Ast.INode<TOut>)await node.Operation(success.Value, CancellationToken);
            return await nextFlow.ExecuteWith(this);
        }
        catch (Exception ex)
        {
            return Outcome.Failure<TOut>(ex);
        }
    }

    internal async Task<Outcome<T[]>> Execute<T>(Ast.Primitive.All<T> node)
    {
        // This implementation waits for all flows to complete. If any have failed, it
        // aggregates all their exceptions. This provides the most comprehensive diagnostic
        // information to the caller, rather than failing fast on the first exception.
        var outcomes = await Task.WhenAll(node.Flows.Select(f => ((Ast.INode<T>)f).ExecuteWith(this)));

        var exceptions = outcomes.OfType<Failure<T>>().Select(f => f.Exception).ToList();

        return exceptions is not []
            ? Outcome.Failure<T[]>(new AggregateException(exceptions))
            : Outcome.Success(outcomes.OfType<Success<T>>().Select(s => s.Value).ToArray());
    }

    internal async Task<Outcome<T>> Execute<T>(Ast.Primitive.Any<T> node) =>
        await TryOperation.TryFindFirstSuccessfulFlow<T>(
            node.Flows.Select(f => ((Ast.INode<T>)f).ExecuteWith(this)).ToList(),
            []);

#endregion

}

internal static class TryOperation
{
    internal static async Task<Outcome<T>> TryFindFirstSuccessfulFlow<T>(List<Task<Outcome<T>>> remainingTasks, List<Exception> accumulatedExceptions)
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

    internal static Task<Outcome<T>> TrySync<T>(Func<T> operation)
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

    internal static async Task<Outcome<T>> TryAsync<T>(Func<Task<T>> operation)
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

    internal static async Task<Outcome<T>> TryCancellableAsync<T>(Func<CancellationToken,Task<T>> operation, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Outcome.Failure<T>(new TaskCanceledException());
        }

        try
        {
            return Outcome.Success(await operation(cancellationToken));
        }
        catch (Exception ex)
        {
            return Outcome.Failure<T>(ex);
        }
    }
}
