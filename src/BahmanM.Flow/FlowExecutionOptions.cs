namespace BahmanM.Flow;

public sealed class FlowExecutionOptions
{
    public CancellationToken CancellationToken { get; init; } = CancellationToken.None;
}
