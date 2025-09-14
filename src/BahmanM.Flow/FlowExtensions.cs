using BahmanM.Flow.Behaviour;
using BahmanM.Flow.Execution;
using BahmanM.Flow.Support;

namespace BahmanM.Flow;

/// <summary>
/// Provides a set of extension methods for composing and transforming <see cref="IFlow{T}"/> instances.
/// These methods are the core operators for building flow-based pipelines.
/// </summary>
public static class FlowExtensions
{
    /// <summary>
    /// Performs a synchronous side-effect if the flow is successful. The original outcome of the flow is unaffected.
    /// This operator is the "Bystander" in the pipeline; it lets you peek at the value without changing it.
    /// </summary>
    /// <remarks>
    /// If the <paramref name="action"/> itself throws an exception, the flow will transition to a 'Failure' state.
    /// Use this for operations like logging or auditing that need to happen on success.
    /// </remarks>
    /// <example>
    /// <code>
    /// var userFlow = Flow
    ///     .Succeed(new User { Name = "John" })
    ///     .DoOnSuccess(user => Console.WriteLine($"User found: {user.Name}"));
    /// </code>
    /// </example>
    /// <typeparam name="T">The type of the value in the flow.</typeparam>
    /// <param name="flow">The source flow.</param>
    /// <param name="action">The synchronous action to perform on the successful value.</param>
    /// <returns>A new <see cref="IFlow{T}"/> that includes the side-effect operation.</returns>
    public static IFlow<T> DoOnSuccess<T>(this IFlow<T> flow, Flow.Operations.DoOnSuccess.Sync<T> action) =>
        new Ast.DoOnSuccess.Sync<T>(flow, action);

    /// <summary>
    /// Performs an asynchronous side-effect if the flow is successful. The original outcome of the flow is unaffected.
    /// </summary>
    /// <inheritdoc cref="DoOnSuccess{T}(IFlow{T}, Flow.Operations.DoOnSuccess.Sync{T})"/>
    public static IFlow<T> DoOnSuccess<T>(this IFlow<T> flow, Flow.Operations.DoOnSuccess.Async<T> asyncAction) =>
        new Ast.DoOnSuccess.Async<T>(flow, asyncAction);

    /// <summary>
    /// Performs a cancellable, asynchronous side-effect if the flow is successful. The original outcome of the flow is unaffected.
    /// </summary>
    /// <inheritdoc cref="DoOnSuccess{T}(IFlow{T}, Flow.Operations.DoOnSuccess.Sync{T})"/>
    public static IFlow<T> DoOnSuccess<T>(this IFlow<T> flow, Flow.Operations.DoOnSuccess.CancellableAsync<T> asyncAction) =>
        new Ast.DoOnSuccess.CancellableAsync<T>(flow, asyncAction);

    /// <summary>
    /// Performs a synchronous side-effect if the flow has failed. The original failure is unaffected.
    /// This operator is the "Bystander" for the failure path; it lets you inspect an exception without handling it.
    /// </summary>
    /// <remarks>
    /// Any exception thrown by the <paramref name="action"/> itself is ignored to ensure the original failure is preserved.
    /// Use this for operations like logging or auditing that need to happen on failure.
    /// </remarks>
    /// <example>
    /// <code>
    /// var failedFlow = Flow
    ///     .Fail&lt;User&gt;(new Exception("Database offline"))
    ///     .DoOnFailure(ex => Console.WriteLine($"An error occurred: {ex.Message}"));
    /// </code>
    /// </example>
    /// <typeparam name="T">The type of the value in the flow.</typeparam>
    /// <param name="flow">The source flow.</param>
    /// <param name="action">The synchronous action to perform on the exception.</param>
    /// <returns>A new <see cref="IFlow{T}"/> that includes the side-effect operation.</returns>
    public static IFlow<T> DoOnFailure<T>(this IFlow<T> flow, Flow.Operations.DoOnFailure.Sync action) =>
        new Ast.DoOnFailure.Sync<T>(flow, action);

    /// <summary>
    /// Performs an asynchronous side-effect if the flow has failed. The original failure is unaffected.
    /// </summary>
    /// <inheritdoc cref="DoOnFailure{T}(IFlow{T}, Flow.Operations.DoOnFailure.Sync)"/>
    public static IFlow<T> DoOnFailure<T>(this IFlow<T> flow, Flow.Operations.DoOnFailure.Async asyncAction) =>
        new Ast.DoOnFailure.Async<T>(flow, asyncAction);

    /// <summary>
    /// Performs a cancellable, asynchronous side-effect if the flow has failed. The original failure is unaffected.
    /// </summary>
    /// <inheritdoc cref="DoOnFailure{T}(IFlow{T}, Flow.Operations.DoOnFailure.Sync)"/>
    public static IFlow<T> DoOnFailure<T>(this IFlow<T> flow, Flow.Operations.DoOnFailure.CancellableAsync asyncAction) =>
        new Ast.DoOnFailure.CancellableAsync<T>(flow, asyncAction);

    /// <summary>
    /// Transforms the successful value of a flow into a new value. This is the "Transformer" operator.
    /// It's the equivalent of LINQ's `Select`.
    /// </summary>
    /// <remarks>
    /// If the source flow has failed, this operation is skipped and the failure is propagated.
    /// If the <paramref name="operation"/> itself throws an exception, the flow will transition to a 'Failure' state.
    /// </remarks>
    /// <example>
    /// <code>
    /// var lengthFlow = Flow
    ///     .Succeed("Hello")
    ///     .Select(s => s.Length); // Resulting flow will yield 5
    /// </code>
    /// </example>
    /// <typeparam name="TIn">The type of the source flow's value.</typeparam>
    /// <typeparam name="TOut">The type of the resulting flow's value.</typeparam>
    /// <param name="flow">The source flow.</param>
    /// <param name="operation">A synchronous function to transform the value.</param>
    /// <returns>A new <see cref="IFlow{TOut}"/> that, upon execution, will apply the transformation to the source flow's successful result.</returns>
    public static IFlow<TOut> Select<TIn, TOut>(this IFlow<TIn> flow, Flow.Operations.Select.Sync<TIn, TOut> operation) =>
        new Ast.Select.Sync<TIn, TOut>(flow, operation);

    /// <summary>
    /// Transforms the successful value of a flow into a new value using an asynchronous operation.
    /// </summary>
    /// <inheritdoc cref="Select{TIn, TOut}(IFlow{TIn}, Flow.Operations.Select.Sync{TIn, TOut})"/>
    public static IFlow<TOut> Select<TIn, TOut>(this IFlow<TIn> flow, Flow.Operations.Select.Async<TIn, TOut> asyncOperation) =>
        new Ast.Select.Async<TIn, TOut>(flow, asyncOperation);

    /// <summary>
    /// Transforms the successful value of a flow into a new value using a cancellable, asynchronous operation.
    /// </summary>
    /// <inheritdoc cref="Select{TIn, TOut}(IFlow{TIn}, Flow.Operations.Select.Sync{TIn, TOut})"/>
    public static IFlow<TOut> Select<TIn, TOut>(this IFlow<TIn> flow, Flow.Operations.Select.CancellableAsync<TIn, TOut> asyncOperation) =>
        new Ast.Select.CancellableAsync<TIn, TOut>(flow, asyncOperation);

    /// <summary>
    /// Sequentially composes the source flow with a subsequent flow. This is the "Sequencer" operator,
    /// and is the primary mechanism for chaining failable operations.
    /// </summary>
    /// <remarks>
    /// This operator is conceptually identical to LINQ's `SelectMany` operator.
    /// <para>
    /// Use <see cref="Chain"/> when your next step is itself a failable operation that returns an <see cref="IFlow{T}"/>.
    /// If the source flow has failed, the <paramref name="operation"/> is skipped and the failure is propagated.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var userFlow = Flow
    ///     .Succeed(123)
    ///     .Chain(id => GetUserFromDatabaseAsync(id));
    ///
    /// IFlow&lt;User&gt; GetUserFromDatabaseAsync(int id)
    /// {
    ///     return Flow.Create(() => _database.Users.FindAsync(id));
    /// }
    /// </code>
    /// </example>
    /// <typeparam name="TIn">The type of the source flow's value.</typeparam>
    /// <typeparam name="TOut">The type of the resulting flow's value.</typeparam>
    /// <param name="flow">The source flow.</param>
    /// <param name="operation">A function that takes the successful result of the source flow and returns the next <see cref="IFlow{TOut}"/> to execute.</param>
    /// <returns>An <see cref="IFlow{TOut}"/> that, upon execution, will take the successful result of the source flow and pass it to the <paramref name="operation"/> function to determine the next flow to execute.</returns>
    public static IFlow<TOut> Chain<TIn, TOut>(this IFlow<TIn> flow, Flow.Operations.Chain.Sync<TIn, TOut> operation) =>
        new Ast.Chain.Sync<TIn, TOut>(flow, operation);

    /// <summary>
    /// Sequentially composes the source flow with a subsequent flow produced by an asynchronous operation.
    /// </summary>
    /// <inheritdoc cref="Chain{TIn, TOut}(IFlow{TIn}, Flow.Operations.Chain.Sync{TIn, TOut})"/>
    public static IFlow<TOut> Chain<TIn, TOut>(this IFlow<TIn> flow, Flow.Operations.Chain.Async<TIn, TOut> asyncOperation) =>
        new Ast.Chain.Async<TIn, TOut>(flow, asyncOperation);

    /// <summary>
    /// Sequentially composes the source flow with a subsequent flow produced by a cancellable, asynchronous operation.
    /// </summary>
    /// <inheritdoc cref="Chain{TIn, TOut}(IFlow{TIn}, Flow.Operations.Chain.Sync{TIn, TOut})"/>
    public static IFlow<TOut> Chain<TIn, TOut>(this IFlow<TIn> flow, Flow.Operations.Chain.CancellableAsync<TIn, TOut> asyncOperation) =>
        new Ast.Chain.CancellableAsync<TIn, TOut>(flow, asyncOperation);

    /// <summary>
    /// Enforces a business rule on the successful value of a flow. This is the "Gatekeeper" operator.
    /// </summary>
    /// <remarks>
    /// If the source flow is successful, the <paramref name="predicate"/> is checked. If the predicate is false,
    /// the flow transitions to a 'Failure' state using the exception returned by the <paramref name="exceptionFactory"/>.
    /// If the source flow has already failed, this operation is skipped.
    /// </remarks>
    /// <example>
    /// <code>
    /// var adminFlow = Flow
    ///     .Succeed(user)
    ///     .Validate(
    ///         user => user.IsAdmin,
    ///         user => new UnauthorizedAccessException($"User '{user.Name}' is not an admin."));
    /// </code>
    /// </example>
    /// <typeparam name="T">The type of the value in the flow.</typeparam>
    /// <param name="flow">The source flow.</param>
    /// <param name="predicate">The synchronous predicate to check against the value.</param>
    /// <param name="exceptionFactory">A function that produces an exception if the predicate is false.</param>
    /// <returns>A new <see cref="IFlow{T}"/> that includes the validation logic.</returns>
    public static IFlow<T> Validate<T>(this IFlow<T> flow, Func<T, bool> predicate, Func<T, Exception> exceptionFactory) =>
        new Ast.Validate.Sync<T>(flow, predicate, exceptionFactory);

    /// <summary>
    /// Enforces a business rule on the successful value of a flow using an asynchronous predicate.
    /// </summary>
    /// <inheritdoc cref="Validate{T}(IFlow{T}, Func{T, bool}, Func{T, Exception})"/>
    public static IFlow<T> Validate<T>(this IFlow<T> flow, Func<T, Task<bool>> predicateAsync, Func<T, Exception> exceptionFactory) =>
        new Ast.Validate.Async<T>(flow, predicateAsync, exceptionFactory);

    /// <summary>
    /// Enforces a business rule on the successful value of a flow using a cancellable, asynchronous predicate.
    /// </summary>
    /// <inheritdoc cref="Validate{T}(IFlow{T}, Func{T, bool}, Func{T, Exception})"/>
    public static IFlow<T> Validate<T>(this IFlow<T> flow, Func<T, CancellationToken, Task<bool>> predicateCancellableAsync, Func<T, Exception> exceptionFactory) =>
        new Ast.Validate.CancellableAsync<T>(flow, predicateCancellableAsync, exceptionFactory);

    /// <summary>
    /// Rescues a failed flow by providing a new flow to execute. This is the "Safety Net" operator.
    /// </summary>
    /// <remarks>
    /// If the source flow has failed, the <paramref name="recover"/> function is called with the exception.
    /// The function should return a new <see cref="IFlow{T}"/> that the engine will execute to continue the pipeline.
    /// This allows for sophisticated recovery logic, such as falling back to a cache or retrying with a different strategy.
    /// If the source flow is already successful, this operation is skipped.
    /// </remarks>
    /// <example>
    /// <code>
    /// var resilientFlow = GetUserFromApi(123)
    ///     .Recover(ex =>
    ///     {
    ///         _logger.LogWarning(ex, "API failed, falling back to cache.");
    ///         return GetUserFromCache(123);
    ///     });
    /// </code>
    /// </example>
    /// <typeparam name="T">The type of the value in the flow.</typeparam>
    /// <param name="flow">The source flow.</param>
    /// <param name="recover">A function that takes the exception from the failed flow and returns a new flow to execute.</param>
    /// <returns>A new <see cref="IFlow{T}"/> that includes the recovery logic.</returns>
    public static IFlow<T> Recover<T>(this IFlow<T> flow, Flow.Operations.Recover.Sync<T> recover) =>
        new Ast.Recover.Sync<T>(flow, recover);

    /// <summary>
    /// Rescues a failed flow by providing a new flow to execute, using an asynchronous recovery function.
    /// </summary>
    /// <inheritdoc cref="Recover{T}(IFlow{T}, Flow.Operations.Recover.Sync{T})"/>
    public static IFlow<T> Recover<T>(this IFlow<T> flow, Flow.Operations.Recover.Async<T> recover) =>
        new Ast.Recover.Async<T>(flow, recover);

    /// <summary>
    /// Rescues a failed flow by providing a new flow to execute, using a cancellable, asynchronous recovery function.
    /// </summary>
    /// <inheritdoc cref="Recover{T}(IFlow{T}, Flow.Operations.Recover.Sync{T})"/>

    /// <summary>
    /// Applies a retry policy to the failable operations within a flow.
    /// </summary>
    /// <remarks>
    /// This behaviour only affects failable nodes in the flow, such as those created by <see cref="Flow.Create{T}(Func{T})"/>
    /// or <see cref="Chain{TIn, TOut}(IFlow{TIn}, Flow.Operations.Chain.Sync{TIn, TOut})"/>. It is a no-op on purely logical operators like <see cref="Select{TIn, TOut}(IFlow{TIn}, Flow.Operations.Select.Sync{TIn, TOut})"/>.
    /// The retry policy will re-execute the failing operation up to <paramref name="maxAttempts"/> times.
    /// </remarks>
    /// <example>
    /// <code>
    /// var resilientFlow = Flow
    ///     .Create(() => CallFlakyApi())
    ///     .WithRetry(3);
    /// </code>
    /// </example>
    /// <typeparam name="T">The type of the value in the flow.</typeparam>
    /// <param name="flow">The source flow.</param>
    /// <param name="maxAttempts">The maximum number of attempts to make.</param>
    /// <param name="nonRetryableExceptions">An optional array of exception types that should not trigger a retry.</param>
    /// <returns>A new <see cref="IFlow{T}"/> with the retry behaviour applied.</returns>
    public static IFlow<T> WithRetry<T>(this IFlow<T> flow, int maxAttempts, params Type[] nonRetryableExceptions)
    {
        var strategy = new RetryStrategy(maxAttempts, nonRetryableExceptions);
        return flow.AsNode().Apply(strategy);
    }

    /// <summary>
    /// Applies a retry policy to the failable operations within a flow, retrying up to a maximum number of attempts.
    /// By default, it will not retry on <see cref="TimeoutException"/>.
    /// </summary>
    /// <inheritdoc cref="WithRetry{T}(IFlow{T}, int, Type[])"/>
    public static IFlow<T> WithRetry<T>(this IFlow<T> flow, int maxAttempts)
    {
        return WithRetry(flow, maxAttempts, typeof(TimeoutException));
    }

    /// <summary>
    /// Applies a timeout policy to the failable operations within a flow.
    /// </summary>
    /// <remarks>
    /// This behaviour wraps failable operations in a timed scope. If the operation does not complete within the specified
    /// <paramref name="duration"/>, it will be cancelled and the flow will transition to a 'Failure' state with a <see cref="TimeoutException"/>.
    /// Like other behaviours, it only affects failable nodes.
    /// </remarks>
    /// <example>
    /// <code>
    /// var timelyFlow = Flow
    ///     .Create(() => LongRunningApiCall())
    ///     .WithTimeout(TimeSpan.FromSeconds(5));
    /// </code>
    /// </example>
    /// <typeparam name="T">The type of the value in the flow.</typeparam>
    /// <param name="flow">The source flow.</param>
    /// <param name="duration">The maximum time allowed for the operation to complete.</param>
    /// <returns>A new <see cref="IFlow{T}"/> with the timeout behaviour applied.</returns>
    public static IFlow<T> WithTimeout<T>(this IFlow<T> flow, TimeSpan duration)
    {
        var strategy = new TimeoutStrategy(duration);
        return flow.AsNode().Apply(strategy);
    }

    /// <summary>
    /// Applies a custom, user-defined behaviour to the flow. This is the generic entry point for all custom cross-cutting concerns.
    /// </summary>
    /// <remarks>
    /// Unlike specific behaviours like <see cref="WithRetry{T}(IFlow{T}, int)"/>, which only apply to failable nodes,
    /// a custom behaviour is applied to every node in the flow. This allows for implementing universal concerns like logging or auditing.
    /// </remarks>
    /// <example>
    /// <code>
    /// public class LoggingBehaviour : IBehaviour
    /// {
    ///     public string OperationType => "Logging";
    ///     public IFlow&lt;T&gt; Apply&lt;T&gt;(IFlow&lt;T&gt; originalFlow)
    ///     {
    ///         return originalFlow.DoOnSuccess(val => Console.WriteLine($"Success: {val}"));
    ///     }
    /// }
    ///
    /// var loggedFlow = Flow
    ///     .Succeed(42)
    ///     .WithBehaviour(new LoggingBehaviour());
    /// </code>
    /// </example>
    /// <typeparam name="T">The type of the value in the flow.</typeparam>
    /// <param name="flow">The source flow.</param>
    /// <param name="behaviour">The custom behaviour to apply.</param>
    /// <returns>A new <see cref="IFlow{T}"/> with the custom behaviour applied.</returns>
    public static IFlow<T> WithBehaviour<T>(this IFlow<T> flow, IBehaviour behaviour)
    {
        var strategy = new CustomBehaviourStrategy(behaviour);
        return flow.AsNode().Apply(strategy);
    }
}
