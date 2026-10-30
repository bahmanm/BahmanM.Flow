using System.Collections.Concurrent;
using BahmanM.Flow.Tests.Support;

namespace BahmanM.Flow.Tests.Integration.Stress;

public class WithTimeout_Outer_Cancellation_Tests
{
    private static readonly TimeSpan TimeoutDuration = TimeSpan.FromMinutes(1);

    [Fact]
    public async Task WithTimeout_OnCancellableLeaf_OuterCancellation_IsTaskCanceled()
    {
        // Arrange
        var sideEffects = new ConcurrentBag<string>();
        using var cts = new CancellationTokenSource();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resource = new DisposableProbe();
        var options = new Execution.Options(cts.Token);
        var operationToken = CancellationToken.None;

        var leaf = Flow.WithResource(
            acquire: () => resource,
            use: _ => Flow.Create<string>(async ct =>
            {
                operationToken = ct;
                started.TrySetResult();
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                    sideEffects.Add("leaf completed");
                    return "done";
                }
                catch (TaskCanceledException)
                {
                    cancellationObserved.TrySetResult();
                    throw;
                }
            }).WithTimeout(TimeoutDuration));

        // Act
        var execution = FlowEngine.ExecuteAsync(leaf, options);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await cts.CancelAsync();
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await Task.WhenAll(cancellationObserved.Task, resource.Disposed)
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        var failure = Assert.IsType<Failure<string>>(outcome);
        Assert.IsType<TaskCanceledException>(failure.Exception);
        Assert.True(operationToken.IsCancellationRequested);
        Assert.True(cancellationObserved.Task.IsCompletedSuccessfully);
        Assert.True(resource.IsDisposed);
        Assert.Empty(sideEffects);
    }
}
