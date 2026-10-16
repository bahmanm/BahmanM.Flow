using BahmanM.Flow.Description.Serialisation;
using Microsoft.Extensions.Time.Testing;

namespace BahmanM.Flow.Description.Tests.Unit.Behaviours;

public class TimeoutDescriptionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Timeout_Description_RetainsApplicationAroundPrimitiveNoOp(bool fail)
    {
        var exception = new InvalidOperationException("primitive failure");
        var source = fail
            ? (Flow.Fail<int>(exception)).AsDescription()
            : (Flow.Succeed(42)).AsDescription();
        var description = ((source).WithTimeout(TimeSpan.Zero)).AsDescription();

        var text = DescriptionFormatter.Describe(description);
        var lowered = description.Lower();
        var outcome = await FlowEngine.ExecuteAsync(description);

        Assert.Contains("Timeout<int>", text);
        Assert.IsType(fail ? typeof(Ast.Primitive.Fail<int>) : typeof(Ast.Primitive.Succeed<int>), lowered);
        if (fail)
            Assert.Same(exception, Assert.IsType<Failure<int>>(outcome).Exception);
        else
            Assert.Equal(Outcome.Success(42), outcome);
    }

    [Fact]
    public async Task Timeout_Description_RetainsDuration_AndDefersOperationUntilExecution()
    {
        // Arrange
        var invocationCount = 0;
        Flow.Operations.Create.Sync<int> operation = () =>
        {
            invocationCount++;
            return 42;
        };
        var source = (Flow.Create<int>(() => operation())).AsDescription();
        var duration = TimeSpan.FromSeconds(5);
        var description = ((source).WithTimeout(duration)).AsDescription();

        // Act
        var descriptionText = DescriptionFormatter.Describe(description);
        var invocationCountAfterInspection = invocationCount;
        var lowered = description.Lower();
        var invocationCountAfterLowering = invocationCount;
        var outcome = await FlowEngine.ExecuteAsync(description);

        // Assert
        Assert.NotEqual(source.Id, description.Id);
        Assert.Contains("Timeout<int>", descriptionText);
        Assert.Contains("duration: 00:00:05", descriptionText);
        Assert.Contains("Create.Sync<int>", descriptionText);
        Assert.Equal(0, invocationCountAfterInspection);
        Assert.Equal(0, invocationCountAfterLowering);
        Assert.IsType<Ast.Create.CancellableAsync<int>>(lowered);
        Assert.Equal(42, Assert.IsType<Success<int>>(outcome).Value);
        Assert.Equal(1, invocationCount);
    }

    [Fact]
    public async Task Timeout_AsyncDescription_DefersOperation_AndTimesOutDuringExecution()
    {
        var timeProvider = new FakeTimeProvider();
        var invocationCount = 0;
        var operationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var operationCompletion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var operationFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Flow.Operations.Create.Async<int> operation = async () =>
        {
            invocationCount++;
            operationStarted.TrySetResult();
            var value = await operationCompletion.Task;
            operationFinished.TrySetResult();
            return value;
        };
        var timeout = TimeSpan.FromMinutes(1);
        var description = (((Flow.Create<int>(() => operation())).AsDescription()).WithTimeout(timeout)).AsDescription();

        // Act
        var descriptionText = DescriptionFormatter.Describe(description);
        var lowered = description.Lower();
        var invocationCountBeforeExecution = invocationCount;
        var options = new Execution.Options(CancellationToken.None) { TimeProvider = timeProvider };
        var execution = FlowEngine.ExecuteAsync(description, options);
        await operationStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        timeProvider.Advance(timeout);
        var outcome = await execution;
        operationCompletion.SetResult(42);
        await operationFinished.Task.WaitAsync(TimeSpan.FromSeconds(2));

        // Assert
        Assert.Contains("Timeout<int>", descriptionText);
        Assert.Contains("Create.Async<int>", descriptionText);
        Assert.IsType<Ast.Create.CancellableAsync<int>>(lowered);
        Assert.Equal(0, invocationCountBeforeExecution);
        Assert.Equal(1, invocationCount);
        Assert.True(operationFinished.Task.IsCompletedSuccessfully);
        Assert.IsType<TimeoutException>(Assert.IsType<Failure<int>>(outcome).Exception);
    }

    [Fact]
    public async Task Timeout_CancellableAsyncDescription_LinksExternalCancellationToOperationToken()
    {
        // Arrange
        using var externalCancellation = new CancellationTokenSource();
        var operationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var operationToken = CancellationToken.None;
        Flow.Operations.Create.CancellableAsync<int> operation = async token =>
        {
            operationToken = token;
            operationStarted.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return 42;
        };
        var description = (((Flow.Create<int>(cancellationToken => operation(cancellationToken))).AsDescription()).WithTimeout(TimeSpan.FromSeconds(5))).AsDescription();

        // Act
        var execution = FlowEngine.ExecuteAsync(
            description,
            new Execution.Options(externalCancellation.Token));
        await operationStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await externalCancellation.CancelAsync();
        var outcome = await execution;

        // Assert
        Assert.NotEqual(externalCancellation.Token, operationToken);
        Assert.True(operationToken.IsCancellationRequested);
        Assert.IsAssignableFrom<OperationCanceledException>(Assert.IsType<Failure<int>>(outcome).Exception);
    }

    [Fact]
    public async Task Timeout_CancellableAsyncDescription_CancelsOperationTokenOnTimeout()
    {
        var timeProvider = new FakeTimeProvider();
        var operationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var operationToken = CancellationToken.None;
        Flow.Operations.Create.CancellableAsync<int> operation = async token =>
        {
            operationToken = token;
            operationStarted.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return 42;
        };
        var timeout = TimeSpan.FromMinutes(1);
        var description = (((Flow.Create<int>(cancellationToken => operation(cancellationToken))).AsDescription()).WithTimeout(timeout)).AsDescription();

        var options = new Execution.Options(CancellationToken.None) { TimeProvider = timeProvider };
        var execution = FlowEngine.ExecuteAsync(description, options);
        await operationStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        timeProvider.Advance(timeout);
        var outcome = await execution;

        // Assert
        Assert.True(operationToken.IsCancellationRequested);
        Assert.IsType<TimeoutException>(Assert.IsType<Failure<int>>(outcome).Exception);
    }

    [Fact]
    public async Task LoweredTimeout_CanBeExecutedConcurrently_WithIndependentTimeProviders()
    {
        var firstTimeProvider = new FakeTimeProvider();
        var secondTimeProvider = new FakeTimeProvider();
        var invocationCount = 0;
        var started = new[]
        {
            new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously),
            new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)
        };
        var completion = new[]
        {
            new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously),
            new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously)
        };
        Flow.Operations.Create.CancellableAsync<int> operation = async token =>
        {
            var index = Interlocked.Increment(ref invocationCount) - 1;
            started[index].TrySetResult();
            return await completion[index].Task.WaitAsync(token);
        };
        var timeout = TimeSpan.FromMinutes(1);
        var description = (((Flow.Create<int>(token => operation(token))).AsDescription()).WithTimeout(timeout)).AsDescription();
        var lowered = description.Lower();

        var firstOptions = new Execution.Options(CancellationToken.None) { TimeProvider = firstTimeProvider };
        var secondOptions = new Execution.Options(CancellationToken.None) { TimeProvider = secondTimeProvider };
        var firstExecution = FlowEngine.ExecuteAsync(lowered, firstOptions);
        var secondExecution = FlowEngine.ExecuteAsync(lowered, secondOptions);
        await Task.WhenAll(started[0].Task, started[1].Task).WaitAsync(TimeSpan.FromSeconds(2));

        firstTimeProvider.Advance(timeout);
        var firstOutcome = await firstExecution.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.IsType<TimeoutException>(Assert.IsType<Failure<int>>(firstOutcome).Exception);
        Assert.False(secondExecution.IsCompleted);

        completion[1].SetResult(42);
        var secondOutcome = await secondExecution.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(42, Assert.IsType<Success<int>>(secondOutcome).Value);
        Assert.Equal(2, invocationCount);
    }

    [Fact(Timeout = 15_000)]
    public async Task Timeout_AroundRetry_BoundsTheWholeRetrySequence()
    {
        var timeProvider = new FakeTimeProvider();
        var attempts = 0;
        var secondAttemptStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var releaseSecondAttempt = new ManualResetEventSlim();
        Flow.Operations.Create.Sync<int> operation = () =>
        {
            attempts++;
            if (attempts == 1)
                throw new InvalidOperationException("try again");

            secondAttemptStarted.TrySetResult();
            releaseSecondAttempt.Wait();
            return 42;
        };
        var source = (Flow.Create<int>(() => operation())).AsDescription();
        var retried = ((source).WithRetry(3)).AsDescription();
        var timeout = TimeSpan.FromMinutes(1);
        var description = ((retried).WithTimeout(timeout)).AsDescription();

        try
        {
            var options = new Execution.Options(CancellationToken.None) { TimeProvider = timeProvider };
            var execution = FlowEngine.ExecuteAsync(description, options);
            await secondAttemptStarted.Task;
            timeProvider.Advance(timeout);
            var outcome = await execution;

            Assert.Equal(2, attempts);
            Assert.IsType<TimeoutException>(Assert.IsType<Failure<int>>(outcome).Exception);
        }
        finally
        {
            releaseSecondAttempt.Set();
        }
    }

    [Fact]
    public async Task Retry_AroundTimeout_DoesNotRetryTimeoutExceptionByDefault()
    {
        var timeProvider = new FakeTimeProvider();
        var attempts = 0;
        var operationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var releaseOperation = new ManualResetEventSlim();
        Flow.Operations.Create.Sync<int> operation = () =>
        {
            attempts++;
            operationStarted.TrySetResult();
            releaseOperation.Wait();
            return 42;
        };
        var source = (Flow.Create<int>(() => operation())).AsDescription();
        var timeout = TimeSpan.FromMinutes(1);
        var timed = ((source).WithTimeout(timeout)).AsDescription();
        var description = ((timed).WithRetry(3)).AsDescription();

        try
        {
            var options = new Execution.Options(CancellationToken.None) { TimeProvider = timeProvider };
            var execution = FlowEngine.ExecuteAsync(description, options);
            await operationStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
            timeProvider.Advance(timeout);
            var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(2));

            Assert.Equal(1, attempts);
            Assert.IsType<TimeoutException>(Assert.IsType<Failure<int>>(outcome).Exception);
        }
        finally
        {
            releaseOperation.Set();
        }
    }
}
