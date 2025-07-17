//========================================================================================
// BahmanM.Flow
//========================================================================================
namespace BahmanM.Flow
{
    // --- Core Interfaces & Types ---
    public interface IFlow<T> { }

    public abstract record Outcome<T>;
    public sealed record Success<T>(T Value) : Outcome<T>;
    public sealed record Failure<T>(Exception Exception) : Outcome<T>;

    // --- Core Factory & Operators ---
    public static class Flow
    {
        // Creation
        public static IFlow<T> Succeed<T>(T value, FlowId flowId = null);
        public static IFlow<T> Fail<T>(Exception error, FlowId flowId = null);
        public static IFlow<T> Create<T>(Func<Outcome<T>> func, FlowId flowId = null);
        public static IFlow<T> Create<T>(Func<Task<Outcome<T>>> asyncFunc, FlowId flowId = null);

        // Concurrency
        public static IFlow<T[]> All<T>(params IFlow<T>[] flows);
        public static IFlow<T> Any<T>(params IFlow<T>[] flows);

        // Resources
        public static IFlow<T> WithResource<TResource, T>(
            Func<TResource> acquire,
            Func<TResource, IFlow<T>> use,
            OperationId operationId = null) where TResource : IDisposable;
    }

    public static class FlowExtensions
    {
        // Core Operators
        public static IFlow<TOut> Select<TIn, TOut>(this IFlow<TIn> flow, Func<TIn, TOut> selector, OperationId operationId = null);
        public static IFlow<TOut> Select<TIn, TOut>(this IFlow<TIn> flow, Func<TIn, Task<TOut>> asyncSelector, OperationId operationId = null);

        public static IFlow<TOut> Chain<TIn, TOut>(this IFlow<TIn> flow, Func<TIn, IFlow<TOut>> func, OperationId operationId = null);
        public static IFlow<TOut> Chain<TIn, TOut>(this IFlow<TIn> flow, Func<TIn, Task<IFlow<TOut>>> asyncFunc, OperationId operationId = null);

        public static IFlow<T> Recover<T>(this IFlow<T> flow, T fallbackValue, OperationId operationId = null);
        public static IFlow<T> Recover<T>(this IFlow<T> flow, Func<Exception, T> recoveryFunc, OperationId operationId = null);
        public static IFlow<T> Recover<T>(this IFlow<T> flow, Func<Exception, IFlow<T>> recoveryFlowFunc, OperationId operationId = null);
        public static IFlow<T> Recover<T>(this IFlow<T> flow, Func<Exception, Task<T>> asyncRecoveryFunc, OperationId operationId = null);
        public static IFlow<T> Recover<T>(this IFlow<T> flow, Func<Exception, Task<IFlow<T>>> asyncRecoveryFlowFunc, OperationId operationId = null);

        // Side-effect Operators
        public static IFlow<T> DoOnSuccess<T>(this IFlow<T> flow, Action<T> action, OperationId operationId = null);
        public static IFlow<T> DoOnSuccess<T>(this IFlow<T> flow, Func<T, Task> asyncAction, OperationId operationId = null);
        public static IFlow<T> DoOnFailure<T>(this IFlow<T> flow, Action<Exception> action, OperationId operationId = null);
        public static IFlow<T> DoOnFailure<T>(this IFlow<T> flow, Func<Exception, Task> asyncAction, OperationId operationId = null);
    }
}

//========================================================================================
// BahmanM.Flow.Annotations
//========================================================================================
namespace BahmanM.Flow.Annotations
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class FlowExperimentalAttribute : Attribute
    {
        public string Message { get; }
        public FlowExperimentalAttribute(string message);
    }
}

//========================================================================================
// BahmanM.Flow.Behaviours
//========================================================================================
namespace BahmanM.Flow.Behaviours
{
    public interface IBehaviour<T>
    {
        string OperationType { get; }
        IFlow<T> Apply(IFlow<T> originalFlow);
    }

    public interface IFlowPolicy { }

    public static class FlowExtensions
    {
        public static IFlow<T> WithBehaviour<T>(this IFlow<T> flow, IBehaviour<T> behaviour, OperationId operationId = null);
    }
}

//========================================================================================
// BahmanM.Flow.Behaviours.Timeout
//========================================================================================
namespace BahmanM.Flow.Behaviours.Timeout
{
    using BahmanM.Flow.Annotations;

    public sealed class FlowTimeoutPolicy : IFlowPolicy
    {
        public FlowTimeoutPolicy(TimeSpan duration);
    }

    public static class FlowExtensions
    {
        [FlowExperimental("The WithTimeout operator is subject to change.")]
        public static IFlow<T> WithTimeout<T>(this IFlow<T> flow, TimeSpan duration, OperationId operationId = null);
        [FlowExperimental("The policy-based WithTimeout operator is subject to change.")]
        public static IFlow<T> WithTimeout<T>(this IFlow<T> flow, FlowTimeoutPolicy policy, OperationId operationId = null);
    }
}

//========================================================================================
// BahmanM.Flow.Behaviours.Retry
//========================================================================================
namespace BahmanM.Flow.Behaviours.Retry
{
    using BahmanM.Flow.Annotations;

    public sealed class FlowRetryPolicy : IFlowPolicy
    {
        public FlowRetryPolicy(int maxAttempts);
    }

    public static class FlowExtensions
    {
        [FlowExperimental("The WithRetry operator is subject to change.")]
        public static IFlow<T> WithRetry<T>(this IFlow<T> flow, int maxAttempts, OperationId operationId = null);
        [FlowExperimental("The policy-based WithRetry operator is subject to change.")]
        public static IFlow<T> WithRetry<T>(this IFlow<T> flow, FlowRetryPolicy policy, OperationId operationId = null);
    }
}

//========================================================================================
// BahmanM.Flow.Execution
//========================================================================================
namespace BahmanM.Flow.Execution
{
    public static class FlowEngine
    {
        public static Task<Outcome<T>> ExecuteAsync<T>(IFlow<T> flow, FlowExecutionOptions options = null);
    }

    public sealed class FlowExecutionOptions
    {
        public CancellationToken CancellationToken { get; init; }
        public IReadOnlyList<IFlowExecutionObserver> Observers { get; init; }
        public static FlowExecutionOptions Default { get; }
    }
}

//========================================================================================
// BahmanM.Flow.Diagnostics
//========================================================================================
namespace BahmanM.Flow.Diagnostics
{
    // --- Observer ---
    public interface IFlowExecutionObserver
    {
        void OnEvent<T>(T e) where T : IFlowEvent;
        void OnFlowStarted(IFlowStartedEvent e) => OnEvent(e);
        void OnOperationStarted(IOperationStartedEvent e) => OnEvent(e);
        void OnOperationSucceeded(IOperationSucceededEvent e) => OnEvent(e);
        void OnOperationFailed(IOperationFailedEvent e) => OnEvent(e);
        void OnFlowSucceeded(IFlowSucceededEvent e) => OnEvent(e);
        void OnFlowFailed(IFlowFailedEvent e) => OnEvent(e);
    }

    // --- Events ---
    public interface IFlowEvent
    {
        FlowId FlowId { get; }
        DateTime Timestamp { get; }
    }

    public interface IFlowStartedEvent : IFlowEvent { }
    public interface IOperationEvent : IFlowEvent
    {
        string OperationType { get; }
        OperationId OperationId { get; }
    }
    public interface IOperationStartedEvent : IOperationEvent { }
    public interface IOperationSucceededEvent : IOperationEvent { }
    public interface IOperationFailedEvent : IOperationEvent
    {
        Exception Exception { get; }
    }
    public interface IFlowCompletedEvent : IFlowEvent { }
    public interface IFlowSucceededEvent : IFlowCompletedEvent { }
    public interface IFlowFailedEvent : IFlowCompletedEvent
    {
        Exception Exception { get; }
    }
}
