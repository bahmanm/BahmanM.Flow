using BahmanM.Flow.Description.Serialisation;

namespace BahmanM.Flow.Description.Tests.Unit.Behaviours;

public class CompositeBehaviourDescriptionTests
{
    [Fact]
    public async Task Retry_Description_AroundAllDoesNotRetryChildren()
    {
        // Arrange
        var firstException = new InvalidOperationException("first failure");
        var secondException = new ArgumentException("second failure");
        var firstExecutions = 0;
        var secondExecutions = 0;
        Flow.Operations.Create.Sync<int> first = () =>
        {
            firstExecutions++;
            throw firstException;
        };
        Flow.Operations.Create.Sync<int> second = () =>
        {
            secondExecutions++;
            throw secondException;
        };
        var all = (Flow.All((Flow.Create(() => first())).AsDescription(), (Flow.Create(() => second())).AsDescription())).AsDescription();
        var description = ((all).WithRetry(3)).AsDescription();

        // Act
        var text = DescriptionFormatter.Describe(description);
        var lowered = description.Lower(BahmanM.Flow.FacadeBehaviourStrategyFactory.Instance);
        var countsAfterLowering = (firstExecutions, secondExecutions);
        var outcome = await FlowEngine.ExecuteAsync(description);

        // Assert
        Assert.Contains("Retry<int[]>", text);
        Assert.Contains("All<int>", text);
        Assert.IsType<Ast.Primitive.All<int>>(lowered);
        Assert.Equal((0, 0), countsAfterLowering);
        var aggregate = Assert.IsType<AggregateException>(Assert.IsType<Failure<int[]>>(outcome).Exception);
        Assert.Equal(2, aggregate.InnerExceptions.Count);
        Assert.Equal(1, firstExecutions);
        Assert.Equal(1, secondExecutions);
    }

    [Fact]
    public async Task Retry_Description_AroundAnyDoesNotRetryChildren()
    {
        // Arrange
        var firstException = new InvalidOperationException("first failure");
        var secondException = new ArgumentException("second failure");
        var firstExecutions = 0;
        var secondExecutions = 0;
        Flow.Operations.Create.Sync<int> first = () =>
        {
            firstExecutions++;
            throw firstException;
        };
        Flow.Operations.Create.Sync<int> second = () =>
        {
            secondExecutions++;
            throw secondException;
        };
        var any = (Flow.Any((Flow.Create(() => first())).AsDescription(), (Flow.Create(() => second())).AsDescription())).AsDescription();
        var description = ((any).WithRetry(3)).AsDescription();

        // Act
        var text = DescriptionFormatter.Describe(description);
        var lowered = description.Lower(BahmanM.Flow.FacadeBehaviourStrategyFactory.Instance);
        var countsAfterLowering = (firstExecutions, secondExecutions);
        var outcome = await FlowEngine.ExecuteAsync(description);

        // Assert
        Assert.Contains("Retry<int>", text);
        Assert.Contains("Any<int>", text);
        Assert.IsType<Ast.Primitive.Any<int>>(lowered);
        Assert.Equal((0, 0), countsAfterLowering);
        var aggregate = Assert.IsType<AggregateException>(Assert.IsType<Failure<int>>(outcome).Exception);
        Assert.Equal(2, aggregate.InnerExceptions.Count);
        Assert.Equal(1, firstExecutions);
        Assert.Equal(1, secondExecutions);
    }

    [Fact]
    public async Task Timeout_Description_AroundAllCancelsEveryBranch()
    {
        // Arrange
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var startedCount = 0;
        var cancelledCount = 0;

        Flow.Operations.Create.CancellableAsync<int> WaitForCancellation() => async token =>
        {
            if (Interlocked.Increment(ref startedCount) == 2)
                started.TrySetResult();

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return 0;
            }
            catch (TaskCanceledException)
            {
                if (token.IsCancellationRequested && Interlocked.Increment(ref cancelledCount) == 2)
                    cancelled.TrySetResult();
                throw;
            }
        };

        var all = (Flow.All((Flow.Create(cancellationToken => WaitForCancellation()(cancellationToken))).AsDescription(), (Flow.Create(cancellationToken => WaitForCancellation()(cancellationToken))).AsDescription())).AsDescription();
        var description = ((all).WithTimeout(TimeSpan.FromMilliseconds(300))).AsDescription();

        // Act
        var text = DescriptionFormatter.Describe(description);
        var lowered = description.Lower(BahmanM.Flow.FacadeBehaviourStrategyFactory.Instance);
        var startedAfterLowering = startedCount;
        var execution = FlowEngine.ExecuteAsync(description);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var outcome = await execution;
        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(2));

        // Assert
        Assert.Contains("Timeout<int[]>", text);
        Assert.Contains("All<int>", text);
        Assert.IsType<Ast.Create.CancellableAsync<int[]>>(lowered);
        Assert.Equal(0, startedAfterLowering);
        Assert.IsType<TimeoutException>(Assert.IsType<Failure<int[]>>(outcome).Exception);
        Assert.Equal(2, startedCount);
        Assert.Equal(2, cancelledCount);
    }

    [Fact]
    public async Task Timeout_Description_AroundAnyCancelsEveryBranchWhenNoneSucceeds()
    {
        // Arrange
        var bothStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var bothCancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var startedCount = 0;
        var cancelledCount = 0;

        Flow.Operations.Create.CancellableAsync<int> WaitForCancellation() => async token =>
        {
            if (Interlocked.Increment(ref startedCount) == 2)
                bothStarted.TrySetResult();

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return 0;
            }
            catch (TaskCanceledException)
            {
                if (Interlocked.Increment(ref cancelledCount) == 2)
                    bothCancelled.TrySetResult();
                throw;
            }
        };

        var any = (Flow.Any((Flow.Create(cancellationToken => WaitForCancellation()(cancellationToken))).AsDescription(), (Flow.Create(cancellationToken => WaitForCancellation()(cancellationToken))).AsDescription())).AsDescription();
        var description = ((any).WithTimeout(TimeSpan.FromMilliseconds(300))).AsDescription();

        // Act
        var text = DescriptionFormatter.Describe(description);
        var lowered = description.Lower(BahmanM.Flow.FacadeBehaviourStrategyFactory.Instance);
        var startsAfterLowering = startedCount;
        var execution = FlowEngine.ExecuteAsync(description);
        await bothStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var outcome = await execution;
        await bothCancelled.Task.WaitAsync(TimeSpan.FromSeconds(2));

        // Assert
        Assert.Contains("Timeout<int>", text);
        Assert.Contains("Any<int>", text);
        Assert.IsType<Ast.Create.CancellableAsync<int>>(lowered);
        Assert.Equal(0, startsAfterLowering);
        Assert.IsType<TimeoutException>(Assert.IsType<Failure<int>>(outcome).Exception);
        Assert.Equal(2, startedCount);
        Assert.Equal(2, cancelledCount);
    }

    [Fact]
    public async Task Timeout_Description_AroundAnyCancelsLosingBranchAfterWinnerSucceeds()
    {
        // Arrange
        var loserStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var loserCancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Flow.Operations.Create.CancellableAsync<int> winner = async _ =>
        {
            await loserStarted.Task;
            return 42;
        };
        Flow.Operations.Create.CancellableAsync<int> loser = async token =>
        {
            loserStarted.TrySetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return -1;
            }
            catch (TaskCanceledException)
            {
                loserCancelled.TrySetResult();
                throw;
            }
        };
        var any = (Flow.Any((Flow.Create(cancellationToken => winner(cancellationToken))).AsDescription(), (Flow.Create(cancellationToken => loser(cancellationToken))).AsDescription())).AsDescription();
        var description = ((any).WithTimeout(TimeSpan.FromSeconds(5))).AsDescription();

        // Act
        var lowered = description.Lower(BahmanM.Flow.FacadeBehaviourStrategyFactory.Instance);
        Assert.False(loserStarted.Task.IsCompleted);
        var outcome = await FlowEngine.ExecuteAsync(description);
        await loserCancelled.Task.WaitAsync(TimeSpan.FromSeconds(2));

        // Assert
        Assert.IsType<Ast.Create.CancellableAsync<int>>(lowered);
        Assert.Equal(Outcome.Success(42), outcome);
    }
}
