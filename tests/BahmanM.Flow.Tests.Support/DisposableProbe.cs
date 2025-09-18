namespace BahmanM.Flow.Tests.Support;

public sealed class DisposableProbe : IDisposable
{
    private int _disposed;
    private readonly TaskCompletionSource _disposedSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool IsDisposed => _disposed != 0;

    public Task Disposed => _disposedSignal.Task;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            _disposedSignal.TrySetResult();
            GC.SuppressFinalize(this);
        }
    }

    public DisposableProbe() { }
}
