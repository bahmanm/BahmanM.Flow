namespace BahmanM.Flow.Execution.Resource.Support;

internal static class AsyncDisposalExtensions
{
    internal static void ObserveFaults(this Task task)
    {
        if (task.IsCompleted)
        {
            _ = task.Exception;
            return;
        }

        _ = task.ContinueWith(
            t => _ = t.Exception,
            TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted);
    }
}
