namespace BahmanM.Flow.Behaviour.Support;

internal static class TimedScope
{
    public static async Task<T> ExecuteAsync<T>(
        TimeSpan thisScopeTimeout,
        TimeProvider timeProvider,
        CancellationToken parentScopeToken,
        Func<CancellationToken, Task<T>> work)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        using var childScopeCts = CancellationTokenSource.CreateLinkedTokenSource(parentScopeToken);
        var childScopeToken = childScopeCts.Token;
        var workTask = StartWork(work, childScopeToken);

        using var timeoutWaitCts = new CancellationTokenSource();
        var timeoutExpiryTask = Task.Delay(thisScopeTimeout, timeProvider, timeoutWaitCts.Token);
        var parentScopeCancellation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var parentScopeCancellationRegistration = parentScopeToken.Register(
            static state => ((TaskCompletionSource<bool>)state!).TrySetResult(true),
            parentScopeCancellation);

        try
        {
            var firstCompletedTask = await Task.WhenAny(
                workTask,
                timeoutExpiryTask,
                parentScopeCancellation.Task).ConfigureAwait(false);

            if (firstCompletedTask == workTask)
                return await workTask.ConfigureAwait(false);

            TryCancel(childScopeCts);

            if (firstCompletedTask == parentScopeCancellation.Task)
                throw new TaskCanceledException();

            workTask.ObserveFaults();
            throw new TimeoutException($"The operation has timed out after {thisScopeTimeout}.");
        }
        finally
        {
            timeoutWaitCts.Cancel();
        }
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
