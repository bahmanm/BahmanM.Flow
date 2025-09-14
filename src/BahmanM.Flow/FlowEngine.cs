using BahmanM.Flow.Execution;
using BahmanM.Flow.Support;

namespace BahmanM.Flow;

/// <summary>
/// The engine responsible for executing an <see cref="IFlow{T}"/>.
/// This class acts as the interpreter for the abstract syntax tree defined by a Flow.
/// </summary>
public static class FlowEngine
{
    /// <summary>
    /// Executes the given Flow and returns its outcome.
    /// This is the entry point for running a Flow that does not require external cancellation.
    /// </summary>
    /// <example>
    /// <code>
    /// var myFlow = Flow.Succeed(42);
    ///
    /// Outcome&lt;int&gt; result = await FlowEngine.ExecuteAsync(myFlow);
    /// </code>
    /// </example>
    /// <typeparam name="T">The type of the value produced by the flow.</typeparam>
    /// <param name="flow">The flow to execute.</param>
    /// <returns>An <see cref="Outcome{T}"/> representing the result of the execution.</returns>
    public static Task<Outcome<T>> ExecuteAsync<T>(IFlow<T> flow) =>
        ExecuteAsync(flow, new Execution.Options(CancellationToken.None));

    /// <summary>
    /// Executes the given Flow with the specified execution options.
    /// </summary>
    /// <remarks>
    /// Use this overload when you need to provide a <see cref="CancellationToken"/> to enable
    /// co-operative cancellation of the Flow from an external source.
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
    /// <param name="options">The execution options, such as a <see cref="CancellationToken"/>.</param>
    public static Task<Outcome<T>> ExecuteAsync<T>(IFlow<T> flow, Execution.Options options) =>
        flow.AsNode().Accept(new Execution.Interpreter(options));
}
