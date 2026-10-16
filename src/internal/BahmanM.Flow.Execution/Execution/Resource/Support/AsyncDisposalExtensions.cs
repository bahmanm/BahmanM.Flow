namespace BahmanM.Flow.Execution.Resource.Support;

internal static class AsyncDisposalExtensions
{
    extension(Task task)
    {
        internal void ObserveFaults()
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
}
