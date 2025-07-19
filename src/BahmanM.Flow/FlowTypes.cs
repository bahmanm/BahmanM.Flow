namespace BahmanM.Flow;

internal sealed record SucceededFlow<T>(T Value) : IFlow<T>;

internal sealed record FailedFlow<T>(Exception Exception) : IFlow<T>;

internal sealed record CreateFlow<T>(Func<T> Operation) : IFlow<T>;