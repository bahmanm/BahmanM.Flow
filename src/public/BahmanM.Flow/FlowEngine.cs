using BahmanM.Flow.Execution;

namespace BahmanM.Flow;

/// <summary>
/// The "Chef" that executes a Flow "recipe".
/// <para>
/// An <see cref="IFlow{T}"/> is a purely declarative data structure that describes a sequence of operations.
/// This engine is the interpreter that walks the description and performs the actual work.
/// </para>
/// </summary>
public static class FlowEngine
{
    /// <summary>
    /// Executes the given Flow and returns its outcome.
    /// This is the entry point for running a Flow that does not require external cancellation.
    /// </summary>
    /// <example>
    /// <code>
    /// // 1. Define a Flow, starting with a failable operation.
    /// var finalResultFlow = Flow
    ///     .Create(() => GetIdFromRequest(request)) // Can fail
    ///     .Chain(id => GetUserFromApiFlow(id))      // Can also fail
    ///     .Select(user => user.Name);               // Transforms the result
    ///
    /// // 2. Execute the entire Flow.
    /// Outcome&lt;string&gt; outcome = await FlowEngine.ExecuteAsync(finalResultFlow);
    ///
    /// // 3. Handle the final outcome.
    /// string result = outcome switch
    /// {
    ///     Success&lt;string&gt; s => $"User's name is {s.Value}",
    ///     Failure&lt;string&gt; f => $"The process failed: {f.Exception.Message}",
    ///     _ => "Unknown outcome"
    /// };
    /// </code>
    /// </example>
    /// <typeparam name="T">The type of the value produced by the Flow.</typeparam>
    /// <param name="flow">The Flow to execute.</param>
    /// <returns>An <see cref="Outcome{T}"/> representing the result of the execution.</returns>
    public static Task<Outcome<T>> ExecuteAsync<T>(IFlow<T> flow) =>
        ExecuteAsync(flow, new Options(CancellationToken.None));

    /// <summary>
    /// Executes the given Flow with the specified execution options.
    /// </summary>
    /// <remarks>
    /// Use this overload to provide execution-scoped dependencies, such as a <see cref="CancellationToken"/>
    /// for co-operative cancellation or a <see cref="TimeProvider"/> for time-related behaviour.
    /// </remarks>
    /// <example>
    /// <code>
    /// var cts = new CancellationTokenSource();
    /// var myFlow = Flow.Create(ct => LongRunningTask(ct));
    ///
    /// // Cancel the flow after 5 seconds
    /// cts.CancelAfter(5000);
    ///
    /// Outcome&lt;int&gt; result = await FlowEngine.ExecuteAsync(myFlow, new(cts.Token));
    /// </code>
    /// </example>
    /// <inheritdoc cref="ExecuteAsync{T}(IFlow{T})"/>
    /// <param name="flow">The Flow to execute.</param>
    /// <param name="options">The execution options, including the cancellation token and time provider.</param>
    public static Task<Outcome<T>> ExecuteAsync<T>(IFlow<T> flow, Options options) =>
        FlowExecutor.ExecuteAsync(flow, options, FacadeBehaviourStrategyFactory.Instance);

    internal static Task<Outcome<T>> ExecuteAsync<T>(Description.IDescription<T> description) =>
        FlowExecutor.ExecuteAsync(description, new Options(CancellationToken.None), FacadeBehaviourStrategyFactory.Instance);

    internal static Task<Outcome<T>> ExecuteAsync<T>(
        Description.IDescription<T> description,
        Options options) =>
        FlowExecutor.ExecuteAsync(description, options, FacadeBehaviourStrategyFactory.Instance);
}
