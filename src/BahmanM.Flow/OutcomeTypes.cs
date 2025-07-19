namespace BahmanM.Flow;

public abstract record Outcome<T>;

public sealed record Success<T> : Outcome<T>
{
    public T Value { get; }
    internal Success(T value) => Value = value;
}

public sealed record Failure<T> : Outcome<T>
{
    public Exception Exception { get; }
    internal Failure(T _, Exception exception) => Exception = exception;
}