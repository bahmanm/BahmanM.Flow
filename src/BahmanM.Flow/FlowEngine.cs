using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow;

public static class FlowEngine
{
    public static Task<Outcome<T>> ExecuteAsync<T>(IFlow<T> flow)
    {
        return flow switch
        {
            SucceededFlow<T> s => Task.FromResult(Success(s.Value)),
            FailedFlow<T> f => Task.FromResult(Failure<T>(f.Exception)),
            CreateFlow<T> c => TryOperation(c.Operation),
            AsyncCreateFlow<T> ac => TryOperation(ac.Operation),
            DoOnSuccessFlow<T> dos => HandleDoOnSuccess(dos.Upstream, dos.Action),
            AsyncDoOnSuccessFlow<T> ados => HandleDoOnSuccess(ados.Upstream, ados.AsyncAction),
            _ => throw new NotSupportedException($"Unsupported source flow type: {flow.GetType().Name}")
        };
    }

    private static async Task<Outcome<T>> HandleDoOnSuccess<T>(IFlow<T> upstream, Action<T> action)
    {
        var upstreamOutcome = await ExecuteAsync(upstream);

        if (upstreamOutcome is Success<T> success)
        {
            try
            {
                action(success.Value);
                return upstreamOutcome;
            }
            catch (Exception ex)
            {
                return Failure<T>(ex);
            }
        }

        return upstreamOutcome;
    }

    private static async Task<Outcome<T>> HandleDoOnSuccess<T>(IFlow<T> upstream, Func<T, Task> asyncAction)
    {
        var upstreamOutcome = await ExecuteAsync(upstream);

        if (upstreamOutcome is Success<T> success)
        {
            try
            {
                await asyncAction(success.Value);
                return upstreamOutcome;
            }
            catch (Exception ex)
            {
                return Failure<T>(ex);
            }
        }

        return upstreamOutcome;
    }

    private static Task<Outcome<T>> TryOperation<T>(Func<T> operation)
    {
        try
        {
            return Task.FromResult(Success(operation()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Failure<T>(ex));
        }
    }

    private static async Task<Outcome<T>> TryOperation<T>(Func<Task<T>> operation)
    {
        try
        {
            return Success(await operation());
        }
        catch (Exception ex)
        {
            return Failure<T>(ex);
        }
    }
}
