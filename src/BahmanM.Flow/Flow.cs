namespace BahmanM.Flow;

public static class Flow
{
    public static IFlow<T> Succeed<T>(T value) => new SucceededFlow<T>(value);

    public static IFlow<T> Fail<T>(Exception exception) => new FailedFlow<T>(exception);

    internal sealed record SucceededFlow<T>(T Value) : IFlow<T>;

    internal sealed record FailedFlow<T>(Exception Exception) : IFlow<T>;
}
