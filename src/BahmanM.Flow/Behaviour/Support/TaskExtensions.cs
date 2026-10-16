namespace BahmanM.Flow.Behaviour.Support;

internal static class TaskExtensions
{
    extension(Task task)
    {
        internal void ObserveFaults()
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
}
