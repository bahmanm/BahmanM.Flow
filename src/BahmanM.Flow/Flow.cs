namespace BahmanM.Flow;

/// <summary>
/// A declarative, asynchronous workflow that encapsulates a sequence of operations,
/// resulting in a value of type <typeparamref name="T"/> or an <see cref="Exception"/>.
/// </summary>
/// <remarks>
/// An <see cref="IFlow{T}"/> is an immutable data structure (an Abstract Syntax Tree) that represents the 'recipe' for a computation.
/// It is not executed until it is passed to the <see cref="FlowEngine"/>.
/// </remarks>
/// <typeparam name="T">The type of the value that will be produced by the successful execution of the flow.</typeparam>
public interface IFlow<T>
{
}

/// <summary>
/// The primary entry point for creating and composing <see cref="IFlow{T}"/> instances.
/// </summary>
public static class Flow
{
    /// <summary>
    /// A flow that starts with a pre-existing, successful value.
    /// This is the simplest way to bring a known value into a flow to begin a pipeline.
    /// </summary>
    /// <example>
    /// <code>
    /// var successfulFlow = Flow.Succeed("Hello, World!");
    /// </code>
    /// </example>
    /// <typeparam name="T">The type of the value encapsulated by the flow.</typeparam>
    /// <param name="value">The successful value to start the flow with.</param>
    /// <returns>An <see cref="IFlow{T}"/> that will immediately yield the provided value when executed.</returns>
    public static IFlow<T> Succeed<T>(T value) => new Ast.Primitive.Succeed<T>(value);

    /// <summary>
    /// A flow that starts in a failed state with a given exception.
    /// </summary>
    /// <example>
    /// <code>
    /// var failedFlow = Flow.Fail&lt;string&gt;(new InvalidOperationException("Something went wrong."));
    /// </code>
    /// </example>
    /// <typeparam name="T">The type of the value that the flow would have produced.</typeparam>
    /// <param name="exception">The exception to start the flow with.</param>
    /// <returns>An <see cref="IFlow{T}"/> that will immediately yield a failure with the provided exception when executed.</returns>
    public static IFlow<T> Fail<T>(Exception exception) => new Ast.Primitive.Fail<T>(exception);

    /// <summary>
    /// A flow from a failable, synchronous operation. The operation is deferred and will only be
    /// executed when the flow is run by the <see cref="FlowEngine"/>.
    /// </summary>
    /// <remarks>
    /// This is the primary way to bring effectful operations (e.g., I/O, database calls) into a flow.
    /// The engine will automatically catch any exceptions thrown by the operation and transition the flow to a 'Failure' state.
    /// </remarks>
    /// <example>
    /// <code>
    /// var dataFlow = Flow.Create(() => file.ReadText());
    /// </code>
    /// </example>
    /// <typeparam name="T">The type of the value produced by the operation.</typeparam>
    /// <param name="operation">A synchronous function that returns a value or throws an exception.</param>
    /// <returns>An <see cref="IFlow{T}"/> that will execute the operation when run.</returns>
    public static IFlow<T> Create<T>(Func<T> operation) => new Ast.Create.Sync<T>(() => operation());

    /// <summary>
    /// A flow from a failable, asynchronous operation. The operation is deferred and will only be
    /// executed when the flow is run by the <see cref="FlowEngine"/>.
    /// </summary>
    /// <inheritdoc cref="Create{T}(Func{T})"/>
    public static IFlow<T> Create<T>(Func<Task<T>> operation) => new Ast.Create.Async<T>(() => operation());

    /// <summary>
    /// A flow from a failable, cancellable asynchronous operation. The operation is deferred and will only be
    /// executed when the flow is run by the <see cref="FlowEngine"/>.
    /// </summary>
    /// <remarks>
    /// Prefer this overload when you want your operation to respect cancellation signals,
    /// especially when using operators like <see cref="Any{T}"/> or when providing a
    /// <see cref="CancellationToken"/> to the <see cref="FlowEngine"/>.
    /// </remarks>
    /// <inheritdoc cref="Create{T}(Func{T})"/>
    public static IFlow<T> Create<T>(Func<CancellationToken, Task<T>> operation) =>
        new Ast.Create.CancellableAsync<T>(operation);

    /// <summary>
    /// A composite flow that runs multiple flows in parallel and collects their results.
    /// It's the <see cref="IFlow{T}"/> equivalent of <see cref="Task.WhenAll(Task[])"/>.
    /// </summary>
    /// <remarks>
    /// If all flows succeed, the resulting flow will succeed with an array of their values.
    /// If any flow fails, the composite flow will fail. The execution behaviour depends on the <see cref="FlowEngine"/>,
    /// which may wait for all flows to complete and return an <see cref="AggregateException"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// var userFlow = GetUserAsync(1);
    /// var permissionsFlow = GetPermissionsAsync(1);
    ///
    /// var combinedFlow = Flow.All(userFlow, permissionsFlow);
    /// // The result will be an IFlow&lt;object[]&gt;
    /// </code>
    /// </example>
    /// <typeparam name="T">The type of value produced by the flows.</typeparam>
    /// <param name="flow">The first flow to include.</param>
    /// <param name="moreFlows">Additional flows to run in parallel.</param>
    /// <returns>An <see cref="IFlow{T}"/> that, upon execution, will run all provided flows in parallel and, if all are successful, yield an array of their results.</returns>
    public static IFlow<T[]> All<T>(IFlow<T> flow, params IFlow<T>[] moreFlows) =>
        new Ast.Primitive.All<T>([flow, ..moreFlows]);

    /// <summary>
    /// A composite flow that races multiple flows against each other and returns the result of the first one to succeed.
    /// It's the <see cref="IFlow{T}"/> equivalent of <see cref="Task.WhenAny(Task[])"/>, but with a focus on success.
    /// </summary>
    /// <remarks>
    /// As soon as one flow succeeds, the other flows are cooperatively cancelled. For this to be effective,
    /// the competing flows should be created with cancellable operations (e.g., using the `Create` overload
    /// that accepts a <see cref="CancellationToken"/>). If all flows fail, the resulting flow will fail with an
    /// <see cref="AggregateException"/> containing all the exceptions.
    /// </remarks>
    /// <example>
    /// <code>
    /// var fromCache = Flow.Create(ct => GetFromCacheAsync("key", ct));
    /// var fromDb = Flow.Create(ct => GetFromDbAsync("key", ct));
    ///
    /// var fastestFlow = Flow.Any(fromCache, fromDb);
    /// </code>
    /// </example>
    /// <typeparam name="T">The type of value produced by the flows.</typeparam>
    /// <param name="flow">The first flow to race.</param>
    /// <param name="moreFlows">Additional flows to race.</param>
    /// <returns>An <see cref="IFlow{T}"/> that, upon execution, will race all provided flows and yield the result of the first one to succeed.</returns>
    public static IFlow<T> Any<T>(IFlow<T> flow, params IFlow<T>[] moreFlows) =>
        new Ast.Primitive.Any<T>([flow, ..moreFlows]);

    /// <summary>
    /// A flow that safely acquires, uses, and disposes of a resource.
    /// It's the <see cref="IFlow{T}"/> equivalent of a <c>using</c> statement.
    /// </summary>
    /// <remarks>
    /// The <paramref name="acquire"/> function is called to get the resource. If it succeeds, the <paramref name="use"/>
    /// function is called with the acquired resource to produce the next flow. Regardless of whether the 'use' flow
    /// succeeds or fails, the resource is guaranteed to be disposed of. If acquiring the resource fails, the entire
    /// flow fails immediately.
    /// </remarks>
    /// <example>
    /// <code>
    /// var contentFlow = Flow.WithResource(
    ///     acquire: () => new StreamReader("data.txt"),
    ///     use: reader => Flow.Create(() => reader.ReadToEnd())
    /// );
    /// </code>
    /// </example>
    /// <typeparam name="TResource">The type of the resource, which must be <see cref="IDisposable"/>.</typeparam>
    /// <typeparam name="T">The type of value produced by the 'use' flow.</typeparam>
    /// <param name="acquire">A function to acquire the disposable resource.</param>
    /// <param name="use">A function that takes the acquired resource and returns the next <see cref="IFlow{T}"/> to execute.</param>
    /// <returns>An <see cref="IFlow{T}"/> that, upon execution, will manage the lifecycle of the resource and run the 'use' flow.</returns>
    public static IFlow<T> WithResource<TResource, T>(Func<TResource> acquire, Func<TResource, IFlow<T>> use)
        where TResource : IDisposable => new Ast.Resource.WithResource<TResource, T>(acquire, use);

    /// <summary>
    /// Contains delegate definitions for the various operations that can be passed to Flow operators.
    /// </summary>
    public static class Operations
    {
        /// <summary>
        /// Delegate definitions for the <see cref="Flow.Create{T}(Func{T})"/> family of methods.
        /// </summary>
        public static class Create
        {
            /// <summary>A synchronous, failable operation that produces a value of type <typeparamref name="T"/>.</summary>
            public delegate T Sync<out T>();
            /// <summary>An asynchronous, failable operation that produces a value of type <typeparamref name="T"/>.</summary>
            public delegate Task<T> Async<T>();
            /// <summary>A cancellable, asynchronous, failable operation that produces a value of type <typeparamref name="T"/>.</summary>
            public delegate Task<T> CancellableAsync<T>(CancellationToken cancellationToken);
        }

        /// <summary>
        /// Delegate definitions for the <see cref="FlowExtensions.Select{TIn, TOut}(IFlow{TIn}, Operations.Select.Sync{TIn, TOut})"/> family of methods.
        /// </summary>
        public static class Select
        {
            /// <summary>A synchronous, failable transformation from <typeparamref name="TIn"/> to <typeparamref name="TOut"/>.</summary>
            public delegate TOut Sync<in TIn, out TOut>(TIn input);
            /// <summary>An asynchronous, failable transformation from <typeparamref name="TIn"/> to <typeparamref name="TOut"/>.</summary>
            public delegate Task<TOut> Async<in TIn, TOut>(TIn input);
            /// <summary>A cancellable, asynchronous, failable transformation from <typeparamref name="TIn"/> to <typeparamref name="TOut"/>.</summary>
            public delegate Task<TOut> CancellableAsync<in TIn, TOut>(TIn input, CancellationToken cancellationToken);
        }

        /// <summary>
        /// Delegate definitions for the <see cref="FlowExtensions.Chain{TIn, TOut}(IFlow{TIn}, Operations.Chain.Sync{TIn, TOut})"/> family of methods.
        /// </summary>
        public static class Chain
        {
            /// <summary>A synchronous operation that takes a value of type <typeparamref name="TIn"/> and returns the next <see cref="IFlow{TOut}"/> in the sequence.</summary>
            public delegate IFlow<TOut> Sync<in TIn, TOut>(TIn input);
            /// <summary>An asynchronous operation that takes a value of type <typeparamref name="TIn"/> and returns the next <see cref="IFlow{TOut}"/> in the sequence.</summary>
            public delegate Task<IFlow<TOut>> Async<in TIn, TOut>(TIn input);
            /// <summary>A cancellable, asynchronous operation that takes a value of type <typeparamref name="TIn"/> and returns the next <see cref="IFlow{TOut}"/> in the sequence.</summary>
            public delegate Task<IFlow<TOut>> CancellableAsync<in TIn, TOut>(TIn input, CancellationToken cancellationToken);
        }

        /// <summary>
        /// Delegate definitions for the <see cref="FlowExtensions.DoOnSuccess{T}(IFlow{T}, Operations.DoOnSuccess.Sync{T})"/> family of methods.
        /// </summary>
        public static class DoOnSuccess
        {
            /// <summary>A synchronous side-effect to perform on a successful value.</summary>
            public delegate void Sync<in T>(T input);
            /// <summary>An asynchronous side-effect to perform on a successful value.</summary>
            public delegate Task Async<in T>(T input);
            /// <summary>A cancellable, asynchronous side-effect to perform on a successful value.</summary>
            public delegate Task CancellableAsync<in T>(T input, CancellationToken cancellationToken);
        }

        /// <summary>
        /// Delegate definitions for the <see cref="FlowExtensions.DoOnFailure{T}(IFlow{T}, Operations.DoOnFailure.Sync)"/> family of methods.
        /// </summary>
        public static class DoOnFailure
        {
            /// <summary>A synchronous side-effect to perform on a failure exception.</summary>
            public delegate void Sync(Exception error);
            /// <summary>An asynchronous side-effect to perform on a failure exception.</summary>
            public delegate Task Async(Exception error);
            /// <summary>A cancellable, asynchronous side-effect to perform on a failure exception.</summary>
            public delegate Task CancellableAsync(Exception error, CancellationToken cancellationToken);
        }

        /// <summary>
        /// Delegate definitions for the <see cref="FlowExtensions.Recover{T}(IFlow{T}, Operations.Recover.Sync{T})"/> family of methods.
        /// </summary>
        public static class Recover
        {
            /// <summary>A synchronous recovery operation that takes an exception and returns a new <see cref="IFlow{T}"/> to continue the workflow.</summary>
            public delegate IFlow<T> Sync<T>(Exception error);
            /// <summary>An asynchronous recovery operation that takes an exception and returns a new <see cref="IFlow{T}"/> to continue the workflow.</summary>
            public delegate Task<IFlow<T>> Async<T>(Exception error);
            /// <summary>A cancellable, asynchronous recovery operation that takes an exception and returns a new <see cref="IFlow{T}"/> to continue the workflow.</summary>
            public delegate Task<IFlow<T>> CancellableAsync<T>(Exception error, CancellationToken cancellationToken);
        }
    }
}
