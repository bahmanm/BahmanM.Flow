namespace BahmanM.Flow;

/// <summary>
/// Factory methods for creating <see cref="Outcome{T}"/> instances.
/// </summary>
public static class Outcome
{
    /// <summary>
    /// Creates a new <see cref="Success{T}"/> outcome.
    /// </summary>
    /// <param name="value">The successful value.</param>
    public static Outcome<T> Success<T>(T value) => new Success<T>(value);

    /// <summary>
    /// Creates a new <see cref="Failure{T}"/> outcome.
    /// </summary>
    /// <param name="exception">The exception that caused the failure.</param>
    public static Outcome<T> Failure<T>(Exception exception) => new Failure<T>(exception);
}

/// <summary>
/// Provides a set of convenience extension methods for working with <see cref="Outcome{T}"/>.
/// </summary>
public static class OutcomeExtensions
{
    /// <summary>
    /// Checks if the outcome is a <see cref="Success{T}"/>.
    /// </summary>
    /// <returns><c>true</c> if the outcome is successful, otherwise <c>false</c>.</returns>
    public static bool IsSuccess<T>(this Outcome<T> outcome) => outcome is Success<T>;

    /// <summary>
    /// Checks if the outcome is a <see cref="Failure{T}"/>.
    /// </summary>
    /// <returns><c>true</c> if the outcome is a failure, otherwise <c>false</c>.</returns>
    public static bool IsFailure<T>(this Outcome<T> outcome) => outcome is Failure<T>;

    /// <summary>
    /// Extracts the value from a <see cref="Success{T}"/> or returns a fallback value if it's a <see cref="Failure{T}"/>.
    /// </summary>
    /// <param name="fallbackValue">The value to return in case of failure.</param>
    /// <returns>The successful value or the fallback value.</returns>
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

/// <summary>
/// The failed outcome of a <see cref="IFlow{T}"/>, containing the exception that occurred.
/// </summary>
/// <param name="Exception">The exception that caused the failure.</param>
/// <typeparam name="T">The type of the value that would have been produced on success.</typeparam>
public sealed record Failure<T>(Exception Exception) : Outcome<T>;

/// <summary>
/// The successful outcome of a <see cref="IFlow{T}"/>, containing the resulting value.
/// </summary>
/// <param name="Value">The result of the successful execution.</param>
/// <typeparam name="T">The type of the successful value.</typeparam>
public sealed record Success<T>(T Value) : Outcome<T>;

/// <summary>
/// The outcome of an executed <see cref="IFlow{T}"/>, which can either
/// be a <see cref="Success{T}"/> or a <see cref="Failure{T}"/>.
/// </summary>
/// <remarks>
/// This is a discriminated union, designed to be handled via pattern matching.
/// </remarks>
/// <typeparam name="T">The type of the value expected from a successful execution.</typeparam>
public abstract record Outcome<T>;
