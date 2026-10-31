using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow.Tests.Integration.Core;

public class WithRetryOperatorNoOpTests
{
    [Fact]
    public async Task WithRetry_AppliedToValidate_DoesNotRetryRejectedPredicate()
    {
        // Arrange
        var syncInvocations = 0;
        var asyncInvocations = 0;
        var cancellableAsyncInvocations = 0;
        var syncException = new InvalidOperationException("Sync validation failed.");
        var asyncException = new InvalidOperationException("Async validation failed.");
        var cancellableAsyncException = new InvalidOperationException("Cancellable async validation failed.");

        Func<int, bool> syncPredicate = _ =>
        {
            syncInvocations++;
            return false;
        };
        Func<int, Task<bool>> asyncPredicate = async _ =>
        {
            asyncInvocations++;
            await Task.Yield();
            return false;
        };
        Func<int, CancellationToken, Task<bool>> cancellableAsyncPredicate = async (_, _) =>
        {
            cancellableAsyncInvocations++;
            await Task.Yield();
            return false;
        };

        var syncFlow = Flow.Succeed(42).Validate(syncPredicate, _ => syncException).WithRetry(3);
        var asyncFlow = Flow.Succeed(42).Validate(asyncPredicate, _ => asyncException).WithRetry(3);
        var cancellableAsyncFlow = Flow.Succeed(42)
            .Validate(cancellableAsyncPredicate, _ => cancellableAsyncException)
            .WithRetry(3);

        // Act
        var syncOutcome = await FlowEngine.ExecuteAsync(syncFlow);
        var asyncOutcome = await FlowEngine.ExecuteAsync(asyncFlow);
        var cancellableAsyncOutcome = await FlowEngine.ExecuteAsync(cancellableAsyncFlow);

        // Assert
        Assert.Equal(Failure<int>(syncException), syncOutcome);
        Assert.Equal(Failure<int>(asyncException), asyncOutcome);
        Assert.Equal(Failure<int>(cancellableAsyncException), cancellableAsyncOutcome);
        Assert.Equal(1, syncInvocations);
        Assert.Equal(1, asyncInvocations);
        Assert.Equal(1, cancellableAsyncInvocations);
    }

    [Fact]
    public async Task WithRetry_AppliedToDoOnSuccess_DoesNotRetryFailingCallback()
    {
        // Arrange
        var syncInvocations = 0;
        var asyncInvocations = 0;
        var cancellableAsyncInvocations = 0;
        var syncException = new InvalidOperationException("Sync success callback failed.");
        var asyncException = new InvalidOperationException("Async success callback failed.");
        var cancellableAsyncException = new InvalidOperationException("Cancellable async success callback failed.");

        Flow.Operations.DoOnSuccess.Sync<int> syncCallback = _ =>
        {
            syncInvocations++;
            throw syncException;
        };
        Flow.Operations.DoOnSuccess.Async<int> asyncCallback = async _ =>
        {
            asyncInvocations++;
            await Task.Yield();
            throw asyncException;
        };
        Flow.Operations.DoOnSuccess.CancellableAsync<int> cancellableAsyncCallback = async (_, _) =>
        {
            cancellableAsyncInvocations++;
            await Task.Yield();
            throw cancellableAsyncException;
        };

        var syncFlow = Flow.Succeed(42).DoOnSuccess(syncCallback).WithRetry(3);
        var asyncFlow = Flow.Succeed(42).DoOnSuccess(asyncCallback).WithRetry(3);
        var cancellableAsyncFlow = Flow.Succeed(42).DoOnSuccess(cancellableAsyncCallback).WithRetry(3);

        // Act
        var syncOutcome = await FlowEngine.ExecuteAsync(syncFlow);
        var asyncOutcome = await FlowEngine.ExecuteAsync(asyncFlow);
        var cancellableAsyncOutcome = await FlowEngine.ExecuteAsync(cancellableAsyncFlow);

        // Assert
        Assert.Equal(Failure<int>(syncException), syncOutcome);
        Assert.Equal(Failure<int>(asyncException), asyncOutcome);
        Assert.Equal(Failure<int>(cancellableAsyncException), cancellableAsyncOutcome);
        Assert.Equal(1, syncInvocations);
        Assert.Equal(1, asyncInvocations);
        Assert.Equal(1, cancellableAsyncInvocations);
    }

    [Fact]
    public async Task WithRetry_AppliedToDoOnFailure_DoesNotRetryFailingCallback()
    {
        // Arrange
        var syncInvocations = 0;
        var asyncInvocations = 0;
        var cancellableAsyncInvocations = 0;
        var syncSourceException = new InvalidOperationException("Sync source failed.");
        var asyncSourceException = new InvalidOperationException("Async source failed.");
        var cancellableAsyncSourceException = new InvalidOperationException("Cancellable async source failed.");
        var syncCallbackException = new InvalidOperationException("Sync failure callback failed.");
        var asyncCallbackException = new InvalidOperationException("Async failure callback failed.");
        var cancellableAsyncCallbackException = new InvalidOperationException("Cancellable async failure callback failed.");

        Flow.Operations.DoOnFailure.Sync syncCallback = _ =>
        {
            syncInvocations++;
            throw syncCallbackException;
        };
        Flow.Operations.DoOnFailure.Async asyncCallback = async _ =>
        {
            asyncInvocations++;
            await Task.Yield();
            throw asyncCallbackException;
        };
        Flow.Operations.DoOnFailure.CancellableAsync cancellableAsyncCallback = async (_, _) =>
        {
            cancellableAsyncInvocations++;
            await Task.Yield();
            throw cancellableAsyncCallbackException;
        };

        var syncFlow = Flow.Fail<int>(syncSourceException).DoOnFailure(syncCallback).WithRetry(3);
        var asyncFlow = Flow.Fail<int>(asyncSourceException).DoOnFailure(asyncCallback).WithRetry(3);
        var cancellableAsyncFlow = Flow.Fail<int>(cancellableAsyncSourceException)
            .DoOnFailure(cancellableAsyncCallback)
            .WithRetry(3);

        // Act
        var syncOutcome = await FlowEngine.ExecuteAsync(syncFlow);
        var asyncOutcome = await FlowEngine.ExecuteAsync(asyncFlow);
        var cancellableAsyncOutcome = await FlowEngine.ExecuteAsync(cancellableAsyncFlow);

        // Assert
        Assert.Equal(Failure<int>(syncSourceException), syncOutcome);
        Assert.Equal(Failure<int>(asyncSourceException), asyncOutcome);
        Assert.Equal(Failure<int>(cancellableAsyncSourceException), cancellableAsyncOutcome);
        Assert.Equal(1, syncInvocations);
        Assert.Equal(1, asyncInvocations);
        Assert.Equal(1, cancellableAsyncInvocations);
    }
}
