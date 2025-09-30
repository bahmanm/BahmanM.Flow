namespace BahmanM.Flow.Behaviour.Support;

internal static class TaskExtensions
{
    internal static void ObserveFaults(this Task task)
    {
        if (task.IsCompleted)
        {
            _ = task.Exception; // observe if faulted
            return;
        }

        _ = task.ContinueWith(
            t => _ = t.Exception,
            TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted);
    }
}
