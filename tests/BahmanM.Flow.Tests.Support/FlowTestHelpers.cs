namespace BahmanM.Flow.Tests.Support;

public static class FlowTestHelpers
{
    public static IFlow<T> NeverCompletesUntilCancelled<T>() =>
        Flow.Create<T>(async ct =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return default!;
        });
}
