namespace BahmanM.Flow;

public static class Flow
{
    public static IFlow<T> Succeed<T>(T value) => new SucceededFlow<T>(value);

    internal sealed record SucceededFlow<T>(T Value) : IFlow<T>;
}
