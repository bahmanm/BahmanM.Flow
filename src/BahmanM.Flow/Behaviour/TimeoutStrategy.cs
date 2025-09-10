using BahmanM.Flow.Support;

namespace BahmanM.Flow.Behaviour;

internal class TimeoutStrategy(TimeSpan duration) : IBehaviourStrategy
{
    public IFlow<T> ApplyTo<T>(Ast.Primitive.Succeed<T> node) => node;
    public IFlow<T> ApplyTo<T>(Ast.Primitive.Fail<T> node) => node;

    public IFlow<T> ApplyTo<T>(Ast.Create.Sync<T> node)
    {
        Flow.Operations.Create.Async<T> newOperation = () => Task.Run(() => node.Operation()).WaitAsync(duration);
        return new Ast.Create.Async<T>(newOperation);
    }

    public IFlow<T> ApplyTo<T>(Ast.Create.Async<T> node)
    {
        Flow.Operations.Create.Async<T> newOperation = () => node.Operation().WaitAsync(duration);
        return new Ast.Create.Async<T>(newOperation);
    }

    public IFlow<T> ApplyTo<T>(Ast.Create.CancellableAsync<T> node)
    {
        Func<CancellationToken, Task<T>> newOperation = ct => node.Operation(ct).WaitAsync(duration, ct);
        return new Ast.Create.CancellableAsync<T>(newOperation);
    }


    public IFlow<T> ApplyTo<T>(Ast.DoOnSuccess.Sync<T> node) =>
        node with { Upstream = node.Upstream.AsNode().Apply(this) };

    public IFlow<T> ApplyTo<T>(Ast.DoOnSuccess.Async<T> node) =>
        node with { Upstream = node.Upstream.AsNode().Apply(this) };

    public IFlow<T> ApplyTo<T>(Ast.DoOnSuccess.CancellableAsync<T> node) =>
        node with { Upstream = node.Upstream.AsNode().Apply(this) };

    public IFlow<T> ApplyTo<T>(Ast.DoOnFailure.Sync<T> node) =>
        node with { Upstream = node.Upstream.AsNode().Apply(this) };

    public IFlow<T> ApplyTo<T>(Ast.DoOnFailure.Async<T> node) =>
        node with { Upstream = node.Upstream.AsNode().Apply(this) };
    public IFlow<T> ApplyTo<T>(Ast.DoOnFailure.CancellableAsync<T> node) =>
        node with { Upstream = node.Upstream.AsNode().Apply(this) };

    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Select.Sync<TIn, TOut> node) =>
        node with { Upstream = node.Upstream.AsNode().Apply(this) };

    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Select.Async<TIn, TOut> node) =>
        node with { Upstream = node.Upstream.AsNode().Apply(this) };

    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Select.CancellableAsync<TIn, TOut> node) =>
        node with { Upstream = node.Upstream.AsNode().Apply(this) };

    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Chain.Sync<TIn, TOut> node)
    {
        Flow.Operations.Chain.Async<TIn, TOut> newOperation = async (value) =>
        {
            var nextFlow = await Task.Run(() => node.Operation(value)).WaitAsync(duration);
            return nextFlow.AsNode().Apply(this);
        };
        return new Ast.Chain.Async<TIn, TOut>(node.Upstream, newOperation);
    }

    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Chain.Async<TIn, TOut> node)
    {
        Flow.Operations.Chain.Async<TIn, TOut> newOperation = async (value) =>
        {
            var nextFlow = await node.Operation(value).WaitAsync(duration);
            return nextFlow.AsNode().Apply(this);
        };
        return node with { Operation = newOperation };
    }

    public IFlow<TOut> ApplyTo<TIn, TOut>(Ast.Chain.CancellableAsync<TIn, TOut> node)
    {
        Flow.Operations.Chain.CancellableAsync<TIn, TOut> newOperation = async (value, cancellationToken) =>
        {
            var nextFlow = await node.Operation(value, cancellationToken).WaitAsync(duration, cancellationToken);
            return nextFlow.AsNode().Apply(this);
        };
        return node with { Operation = newOperation };
    }

    public IFlow<T> ApplyTo<T>(Ast.Recover.Sync<T> node) =>
        node with { Source = node.Source.AsNode().Apply(this) };

    public IFlow<T> ApplyTo<T>(Ast.Recover.Async<T> node) =>
        node with { Source = node.Source.AsNode().Apply(this) };

    public IFlow<T> ApplyTo<T>(Ast.Recover.CancellableAsync<T> node) =>
        node with { Source = node.Source.AsNode().Apply(this) };

    public IFlow<T[]> ApplyTo<T>(Ast.Primitive.All<T> node)
    {
        Func<CancellationToken, Task<T[]>> newOperation = parentScopeToken =>
            TimedScope.ExecuteAsync(
                duration,
                parentScopeToken,
                childScopeToken => FlowEngine
                    .ExecuteAsync(node, new Execution.Options(childScopeToken))
                    .Unwrap());
        return new Ast.Create.CancellableAsync<T[]>(newOperation);
    }

    public IFlow<T> ApplyTo<T>(Ast.Primitive.Any<T> node)
    {
        Func<CancellationToken, Task<T>> newOperation = parentScopeToken =>
            TimedScope.ExecuteAsync(
                duration,
                parentScopeToken,
                childScopeToken => FlowEngine
                    .ExecuteAsync(node, new Execution.Options(childScopeToken))
                    .Unwrap());
        return new Ast.Create.CancellableAsync<T>(newOperation);
    }

    public IFlow<T> ApplyTo<T>(Ast.Validate.Sync<T> node) =>
        node with { Upstream = node.Upstream.AsNode().Apply(this) };

    public IFlow<T> ApplyTo<T>(Ast.Validate.Async<T> node) =>
        node with { Upstream = node.Upstream.AsNode().Apply(this) };

    public IFlow<T> ApplyTo<T>(Ast.Validate.CancellableAsync<T> node) =>
        node with { Upstream = node.Upstream.AsNode().Apply(this) };

    public IFlow<T> ApplyTo<TResource, T>(Ast.Resource.WithResource<TResource, T> node) where TResource : IDisposable => node;

    private static class TimedScope
    {
        public static async Task<T> ExecuteAsync<T>(
            TimeSpan thisScopeTimeout,
            CancellationToken parentScopeToken,
            Func<CancellationToken, Task<T>> work)
        {
            using var childScopeCts = CancellationTokenSource.CreateLinkedTokenSource(parentScopeToken);
            var childScopeToken = childScopeCts.Token;
            var workTask = StartWork(work, childScopeToken);

            var timeoutExpiryTask = Task.Delay(thisScopeTimeout);
            var parentScopeCancellationTask = Task.Delay(Timeout.InfiniteTimeSpan, parentScopeToken);

            var firstCompletedTask = await Task.WhenAny(workTask, timeoutExpiryTask, parentScopeCancellationTask).ConfigureAwait(false);

            if (firstCompletedTask == workTask)
                return await workTask.ConfigureAwait(false);

            TryCancel(childScopeCts);

            if (firstCompletedTask == parentScopeCancellationTask)
                throw new TaskCanceledException();

            ObserveFaults(workTask);
            throw new TimeoutException($"The operation has timed out after {thisScopeTimeout}.");
        }

        private static Task<T> StartWork<T>(Func<CancellationToken, Task<T>> w, CancellationToken ct)
        {
            try { return w(ct); }
            catch (Exception ex) { return Task.FromException<T>(ex); }
        }

        private static void TryCancel(CancellationTokenSource cts)
        {
            try { cts.Cancel(); } catch { /* best-effort */ }
        }

        private static void ObserveFaults(Task t)
        {
            if (t.IsCompleted) return;
            _ = t.ContinueWith(
                tt => _ = tt.Exception,
                TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
