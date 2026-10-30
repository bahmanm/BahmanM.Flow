using System.Collections.Concurrent;

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
        var options = new Execution.Options(cts.Token);
        var operationToken = CancellationToken.None;

        var leaf = Flow.Create<string>(async ct =>
        {
            operationToken = ct;
            started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            sideEffects.Add("leaf completed");
            return "done";
        }).WithTimeout(TimeoutDuration);

        // Act
        var execution = FlowEngine.ExecuteAsync(leaf, options);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await cts.CancelAsync();
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        var failure = Assert.IsType<Failure<string>>(outcome);
        Assert.IsType<TaskCanceledException>(failure.Exception);
        Assert.True(operationToken.IsCancellationRequested);
        Assert.Empty(sideEffects);
    }
}
