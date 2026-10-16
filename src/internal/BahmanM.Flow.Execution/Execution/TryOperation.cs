namespace BahmanM.Flow.Execution;

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

    internal static Task<Outcome<T>> Sync<T>(Func<T> operation)
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

    internal static async Task<Outcome<T>> Async<T>(Func<Task<T>> operation)
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

    internal static async Task<Outcome<T>> CancellableAsync<T>(Func<CancellationToken,Task<T>> operation, CancellationToken cancellationToken)
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
