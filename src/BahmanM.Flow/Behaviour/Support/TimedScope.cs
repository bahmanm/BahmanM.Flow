namespace BahmanM.Flow.Behaviour.Support;

internal static class TimedScope
{
    public static async Task<T> ExecuteAsync<T>(
        TimeSpan thisScopeTimeout,
        CancellationToken parentScopeToken,
        Func<CancellationToken, Task<T>> work)
    {
        using var childScopeCts = CancellationTokenSource.CreateLinkedTokenSource(parentScopeToken);
        var childScopeToken = childScopeCts.Token;
        var workTask = StartWork(work, childScopeToken);

        var timeoutExpiryTask = Task.Delay(thisScopeTimeout);
        var parentScopeCancellationTask = Task.Delay(Timeout.InfiniteTimeSpan, parentScopeToken);

        var firstCompletedTask = await Task.WhenAny(workTask, timeoutExpiryTask, parentScopeCancellationTask).ConfigureAwait(false);

        if (firstCompletedTask == workTask)
            return await workTask.ConfigureAwait(false);

        TryCancel(childScopeCts);

        if (firstCompletedTask == parentScopeCancellationTask)
            throw new TaskCanceledException();

        workTask.ObserveFaults();
        throw new TimeoutException($"The operation has timed out after {thisScopeTimeout}.");
    }

    private static Task<T> StartWork<T>(Func<CancellationToken, Task<T>> w, CancellationToken ct)
    {
        try
        {
            return w(ct);
        }
        catch (Exception ex)
        {
            return Task.FromException<T>(ex);
        }
    }

    private static void TryCancel(CancellationTokenSource cts)
    {
        try
        {
            cts.Cancel();
        }
        catch
        {
             /* best-effort */
        }
    }
}
