namespace BahmanM.Flow;

public static class OutcomeExtensions
{
    public static bool IsSuccess<T>(this Outcome<T> outcome) => outcome is Success<T>;
    public static bool IsFailure<T>(this Outcome<T> outcome) => outcome is Failure<T>;

    public static T GetOrElse<T>(this Outcome<T> outcome, T fallbackValue) =>
        outcome is Success<T> s ? s.Value : fallbackValue;
}
