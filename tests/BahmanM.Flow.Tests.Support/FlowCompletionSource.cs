namespace BahmanM.Flow.Tests.Support;

public sealed class FlowCompletionSource<T>
{
    private readonly TaskCompletionSource _started =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<T> _result =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task Started => _started.Task;

    public IFlow<T> Flow => BahmanM.Flow.Flow.Create<T>(async () =>
    {
        _started.TrySetResult();
        return await _result.Task.ConfigureAwait(false);
    });

    public void Succeed(T value) => _result.TrySetResult(value);
    public void Fail(Exception ex) => _result.TrySetException(ex);
    public void Cancel() => _result.TrySetCanceled();
}
