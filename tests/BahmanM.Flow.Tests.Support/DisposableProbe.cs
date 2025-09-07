namespace BahmanM.Flow.Tests.Support;

public sealed class DisposableProbe : IDisposable
{
    private int _disposed;
    public Guid Id { get; } = Guid.NewGuid();
    public bool IsDisposed => _disposed != 0;

    public static int AllocatedCount;
    public static int DisposedCount;
    public static int FinalizedWithoutDisposeCount;

    public DisposableProbe()
    {
        Interlocked.Increment(ref AllocatedCount);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            Interlocked.Increment(ref DisposedCount);
        }
    }

    ~DisposableProbe()
    {
        if (!IsDisposed)
        {
            Interlocked.Increment(ref FinalizedWithoutDisposeCount);
        }
    }

    public static void Reset()
    {
        AllocatedCount = 0;
        DisposedCount = 0;
        FinalizedWithoutDisposeCount = 0;
    }
}
