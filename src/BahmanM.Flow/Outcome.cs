namespace BahmanM.Flow;

public static class Outcome
{
    public static Outcome<T> Success<T>(T value) => new Success<T>(value);
    public static Outcome<T> Failure<T>(Exception exception) => new Failure<T>(exception);
}

public static class OutcomeExtensions
{
    public static bool IsSuccess<T>(this Outcome<T> outcome) => outcome is Success<T>;
    public static bool IsFailure<T>(this Outcome<T> outcome) => outcome is Failure<T>;

    public static T GetOrElse<T>(this Outcome<T> outcome, T fallbackValue) =>
        outcome is Success<T> s ? s.Value : fallbackValue;

    internal static async Task<T> Unwrap<T>(this Task<Outcome<T>> outcomeTask)
    {
        var outcome = await outcomeTask;
        return outcome switch
        {
            Success<T> s => s.Value,
            Failure<T> f => throw f.Exception,
            _ => throw new NotSupportedException("Unsupported outcome type.")
        };
    }
}

public sealed record Failure<T>(Exception Exception) : Outcome<T>;

public sealed record Success<T>(T Value) : Outcome<T>;

public abstract record Outcome<T>;
