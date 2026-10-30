using BahmanM.Flow.Tests.Support;

namespace BahmanM.Flow.Tests.Integration.Stress;

public class AllCancellationTests
{
    [Fact]
    public async Task All_Scale_OuterCancellation_CancelsBranchesAndDisposesResources()
    {
        // Arrange
        const int branchCount = 50;
        using var cts = new CancellationTokenSource();
        var options = new Execution.Options(cts.Token);
        var started = Enumerable
            .Range(0, branchCount)
            .Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously))
            .ToArray();
        var cancellationObserved = Enumerable
            .Range(0, branchCount)
            .Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously))
            .ToArray();
        var exited = Enumerable
            .Range(0, branchCount)
            .Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously))
            .ToArray();
        var resources = Enumerable
            .Range(0, branchCount)
            .Select(_ => new DisposableProbe())
            .ToArray();
        var branches = Enumerable
            .Range(0, branchCount)
            .Select(index => Flow.WithResource(
                acquire: () => resources[index],
                use: _ => Flow.Create(async ct =>
                {
                    started[index].TrySetResult();
                    try
                    {
                        await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                        return index;
                    }
                    catch (TaskCanceledException)
                    {
                        cancellationObserved[index].TrySetResult();
                        throw;
                    }
                    finally
                    {
                        exited[index].TrySetResult();
                    }
                })))
            .ToArray();

        // Act
        var execution = FlowEngine.ExecuteAsync(Flow.All(branches[0], branches[1..]), options);
        await Task.WhenAll(started.Select(signal => signal.Task))
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await cts.CancelAsync();
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await Task.WhenAll(
                Task.WhenAll(cancellationObserved.Select(signal => signal.Task)),
                Task.WhenAll(exited.Select(signal => signal.Task)),
                Task.WhenAll(resources.Select(resource => resource.Disposed)))
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        var failure = Assert.IsType<Failure<int[]>>(outcome);
        var aggregate = Assert.IsType<AggregateException>(failure.Exception);
        Assert.Equal(branchCount, aggregate.InnerExceptions.Count);
        Assert.All(aggregate.InnerExceptions, exception => Assert.IsType<TaskCanceledException>(exception));
        Assert.All(cancellationObserved, signal => Assert.True(signal.Task.IsCompletedSuccessfully));
        Assert.All(exited, signal => Assert.True(signal.Task.IsCompletedSuccessfully));
        Assert.All(resources, resource => Assert.True(resource.IsDisposed));
    }
}
