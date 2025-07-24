namespace BahmanM.Flow;

public static class Outcome
{
    public static Outcome<T> Success<T>(T value) => new Success<T>(value);
    public static Outcome<T> Failure<T>(Exception exception) => new Failure<T>(exception);
}
