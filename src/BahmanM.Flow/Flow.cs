namespace BahmanM.Flow;

public static class Flow
{
    public static IFlow<T> Succeed<T>(T value) => new SucceededFlow<T>(value);

    public static IFlow<T> Fail<T>(Exception exception) => new FailedFlow<T>(exception);

    public static IFlow<T> Create<T>(Func<T> operation) => new CreateFlow<T>(operation);

    public static IFlow<T> Create<T>(Func<Task<T>> operation) => new AsyncCreateFlow<T>(operation);
}
