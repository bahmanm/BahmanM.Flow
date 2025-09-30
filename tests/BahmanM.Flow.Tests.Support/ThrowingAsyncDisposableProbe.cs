namespace BahmanM.Flow.Tests.Support;

public sealed class ThrowingAsyncDisposableProbe(Exception? exception = null, TimeSpan? delay = null) : IAsyncDisposable
{
    private readonly TimeSpan _delay = delay ?? TimeSpan.Zero;
    private readonly Exception _exception = exception ?? new InvalidOperationException("Async disposal failed.");

    public async ValueTask DisposeAsync()
    {
        if (_delay > TimeSpan.Zero)
            await Task.Delay(_delay).ConfigureAwait(false);

        throw _exception;
    }
}
