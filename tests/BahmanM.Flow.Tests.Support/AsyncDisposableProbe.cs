namespace BahmanM.Flow.Tests.Support;

public sealed class AsyncDisposableProbe(TimeSpan? delay = null) : IAsyncDisposable
{
    private readonly TimeSpan _delay = delay ?? TimeSpan.Zero;
    private int _disposed;
    private readonly TaskCompletionSource _disposedSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool IsDisposed => _disposed != 0;
    public Task Disposed => _disposedSignal.Task;

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        if (_delay > TimeSpan.Zero)
            await Task.Delay(_delay).ConfigureAwait(false);

        _disposedSignal.TrySetResult();
        GC.SuppressFinalize(this);
    }
}
