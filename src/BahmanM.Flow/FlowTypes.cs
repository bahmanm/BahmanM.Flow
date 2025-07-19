namespace BahmanM.Flow;

internal sealed record SucceededFlow<T>(T Value) : IFlow<T>;

internal sealed record FailedFlow<T>(Exception Exception) : IFlow<T>;

internal sealed record CreateFlow<T>(Func<T> Operation) : IFlow<T>;

internal sealed record AsyncCreateFlow<T>(Func<Task<T>> Operation) : IFlow<T>;

internal sealed record DoOnSuccessFlow<T>(IFlow<T> Upstream, Action<T> Action) : IFlow<T>;

internal sealed record AsyncDoOnSuccessFlow<T>(IFlow<T> Upstream, Func<T, Task> AsyncAction) : IFlow<T>;
