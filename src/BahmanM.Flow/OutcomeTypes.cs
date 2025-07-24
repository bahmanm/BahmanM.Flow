namespace BahmanM.Flow;

public abstract record Outcome<T>;

public sealed record Success<T>(T Value) : Outcome<T>;

public sealed record Failure<T>(Exception Exception) : Outcome<T>;