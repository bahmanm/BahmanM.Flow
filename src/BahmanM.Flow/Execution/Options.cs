namespace BahmanM.Flow.Execution;

/// <summary>
/// Provides execution options for a <see cref="FlowEngine"/> run.
/// </summary>
/// <param name="CancellationToken">
/// The token to monitor for cancellation requests. This enables cooperative cancellation of the Flow.
/// </param>
public sealed record Options(CancellationToken CancellationToken);
