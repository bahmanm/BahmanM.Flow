namespace BahmanM.Flow.Execution;

/// <summary>
/// Provides execution options for a <see cref="FlowEngine"/> run.
/// </summary>
/// <param name="CancellationToken">
/// The token to monitor for cancellation requests. This enables cooperative cancellation of the Flow.
/// </param>
public sealed record Options(CancellationToken CancellationToken)
{
    /// <summary>
    /// Gets the provider used for time-related execution behaviour, such as <c>WithTimeout</c>.
    /// Defaults to <see cref="System.TimeProvider.System"/>.
    /// </summary>
    public TimeProvider TimeProvider
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(TimeProvider));
    } = TimeProvider.System;
}
