using Microsoft.Extensions.Time.Testing;

using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow.Tests.Integration.Core;

public class WithTimeoutTests
{
    private const string SimoneDeBeauvoir = "Simone de Beauvoir";
    private static readonly TimeSpan TimeoutDuration = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

    public class NonFailableFlowsTheoryData : TheoryData<IFlow<string>>
    {
        public NonFailableFlowsTheoryData()
        {
            Add(Flow.Succeed("succeeded"));
            Add(Flow.Fail<string>(new Exception("dummy")));
            Add(Flow.Succeed("s").Select(_ => "selected"));
            Add(Flow.Succeed("s").Select<string, string>(async _ =>
            {
                await Task.Yield();
                return "async selected";
            }));
            Add(Flow.Succeed("s").DoOnSuccess(_ => { }));
            Add(Flow.Succeed("s").DoOnSuccess(async _ => await Task.Yield()));
            Add(Flow.Succeed("s").DoOnFailure(_ => { }));
            Add(Flow.Succeed("s").DoOnFailure(async _ => await Task.Yield()));
        }
    }

    [Theory]
    [ClassData(typeof(NonFailableFlowsTheoryData))]
    public async Task WithTimeout_OnNonFailableNodes_IsANoOpAndPreservesOutcome(IFlow<string> nonFailableFlow)
    {
        // Arrange
        var originalFlow = nonFailableFlow;
        var resultFlow = originalFlow.WithTimeout(TimeoutDuration);

        // Act
        var originalOutcome = await FlowEngine.ExecuteAsync(originalFlow);
        var resultOutcome = await FlowEngine.ExecuteAsync(resultFlow);

        // Assert
        Assert.Equal(originalOutcome, resultOutcome);
    }

    [Fact]
    public async Task WithTimeout_WhenOperationExceedsDuration_FailsWithTimeoutException()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        var timedFlow = CreateNeverCompletingFlow().WithTimeout(TimeoutDuration);

        // Act
        var execution = FlowEngine.ExecuteAsync(timedFlow, CreateOptions(timeProvider));
        Assert.False(execution.IsCompleted);
        timeProvider.Advance(TimeoutDuration);
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        var failure = Assert.IsType<Failure<string>>(outcome);
        Assert.IsType<TimeoutException>(failure.Exception);
    }

    [Fact]
    public async Task WithTimeout_WhenOperationCompletesWithinDuration_Succeeds()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        var operationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var flow = Flow.Create<string>(_ =>
        {
            operationStarted.TrySetResult();
            return completion.Task;
        }).WithTimeout(TimeoutDuration);

        // Act
        var execution = FlowEngine.ExecuteAsync(flow, CreateOptions(timeProvider));

        await operationStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        timeProvider.Advance(TimeSpan.FromSeconds(30));
        completion.SetResult(SimoneDeBeauvoir);

        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(Success(SimoneDeBeauvoir), outcome);
    }

    [Fact]
    public async Task WithTimeout_WhenAsyncChainExceedsDuration_FailsWithTimeoutException()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        var flow = Flow.Succeed("start").Chain(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return Flow.Succeed(SimoneDeBeauvoir);
        });

        // Act
        var execution = FlowEngine.ExecuteAsync(flow.WithTimeout(TimeoutDuration), CreateOptions(timeProvider));
        Assert.False(execution.IsCompleted);
        timeProvider.Advance(TimeoutDuration);
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        var failure = Assert.IsType<Failure<string>>(outcome);
        Assert.IsType<TimeoutException>(failure.Exception);
    }

    [Fact]
    public async Task WithTimeout_WhenSyncOperationExceedsDuration_FailsWithTimeoutException()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        using var operationStarted = new ManualResetEventSlim();
        using var releaseOperation = new ManualResetEventSlim();
        var flow = Flow.Create<string>(() =>
        {
            operationStarted.Set();
            releaseOperation.Wait();
            return SimoneDeBeauvoir;
        }).WithTimeout(TimeoutDuration);

        // Act
        var execution = FlowEngine.ExecuteAsync(flow, CreateOptions(timeProvider));

        try
        {
            Assert.True(operationStarted.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            timeProvider.Advance(TimeoutDuration);

            var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            // Assert
            var failure = Assert.IsType<Failure<string>>(outcome);
            Assert.IsType<TimeoutException>(failure.Exception);
        }
        finally
        {
            releaseOperation.Set();
        }
    }

    [Fact]
    public async Task WithTimeout_AfterSelect_TimesOutUpstreamCreate()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var selectorInvocations = 0;
        var flow = CreateNeverCompletingFlow(started)
            .Select(value =>
            {
                selectorInvocations++;
                return value;
            })
            .WithTimeout(TimeoutDuration);

        // Act
        var execution = FlowEngine.ExecuteAsync(flow, CreateOptions(timeProvider));
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        timeProvider.Advance(TimeoutDuration);
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        var failure = Assert.IsType<Failure<string>>(outcome);
        Assert.IsType<TimeoutException>(failure.Exception);
        Assert.Equal(0, selectorInvocations);
    }

    [Fact]
    public async Task WithTimeout_AfterSelect_DoesNotTimeAsyncSelector()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        var selectorStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var selectorCompletion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var flow = Flow.Succeed("input")
            .Select((Flow.Operations.Select.Async<string, string>)(_ =>
            {
                selectorStarted.TrySetResult();
                return selectorCompletion.Task;
            }))
            .WithTimeout(TimeoutDuration);

        // Act
        var execution = FlowEngine.ExecuteAsync(flow, CreateOptions(timeProvider));
        await selectorStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        timeProvider.Advance(TimeoutDuration);
        var executionRemainsPending = !execution.IsCompleted;

        selectorCompletion.SetResult("selected");
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(executionRemainsPending);
        Assert.Equal(Success("selected"), outcome);
    }

    [Fact]
    public async Task WithTimeout_AfterValidate_TimesOutUpstreamCreate()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var predicateInvocations = 0;
        var flow = CreateNeverCompletingFlow(started)
            .Validate(_ =>
            {
                predicateInvocations++;
                return true;
            }, _ => new InvalidOperationException("Validation failed."))
            .WithTimeout(TimeoutDuration);

        // Act
        var execution = FlowEngine.ExecuteAsync(flow, CreateOptions(timeProvider));
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        timeProvider.Advance(TimeoutDuration);
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        var failure = Assert.IsType<Failure<string>>(outcome);
        Assert.IsType<TimeoutException>(failure.Exception);
        Assert.Equal(0, predicateInvocations);
    }

    [Fact]
    public async Task WithTimeout_AfterValidate_DoesNotTimeCancellableAsyncPredicate()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        var predicateStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var predicateCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var predicateInvocations = 0;
        var flow = Flow.Succeed(42)
            .Validate(async (_, _) =>
            {
                predicateInvocations++;
                predicateStarted.TrySetResult();
                return await predicateCompletion.Task;
            }, _ => new InvalidOperationException("Validation failed."))
            .WithTimeout(TimeoutDuration);

        // Act
        var execution = FlowEngine.ExecuteAsync(flow, CreateOptions(timeProvider));
        await predicateStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        timeProvider.Advance(TimeoutDuration);
        var executionRemainsPending = !execution.IsCompleted;
        predicateCompletion.SetResult(true);
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(executionRemainsPending);
        Assert.Equal(Success(42), outcome);
        Assert.Equal(1, predicateInvocations);
    }

    [Fact]
    public async Task WithRetryThenWithTimeout_WhenRetriesSucceedWithinTimeout_Succeeds()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        var firstDelayStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondDelayStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var attempts = 0;
        var flow = Flow.Create<string>(async cancellationToken =>
        {
            attempts++;
            if (attempts < 3)
            {
                var delay = Task.Delay(RetryDelay, timeProvider, cancellationToken);
                (attempts == 1 ? firstDelayStarted : secondDelayStarted).TrySetResult();
                await delay;
                throw new InvalidOperationException($"Attempt {attempts} failed");
            }

            return SimoneDeBeauvoir;
        });

        // Act
        var execution = FlowEngine.ExecuteAsync(
            flow.WithRetry(3).WithTimeout(TimeoutDuration),
            CreateOptions(timeProvider));

        await firstDelayStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        timeProvider.Advance(RetryDelay);
        await secondDelayStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        timeProvider.Advance(RetryDelay);

        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, attempts);
        Assert.Equal(Success(SimoneDeBeauvoir), outcome);
    }

    [Fact]
    public async Task WithTimeoutThenWithRetry_WhenFirstAttemptTimesOut_DoesNotRetry()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        var attempts = 0;
        var flow = Flow.Create<string>(async cancellationToken =>
        {
            attempts++;
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return SimoneDeBeauvoir;
        });

        // Act
        var execution = FlowEngine.ExecuteAsync(
            flow.WithTimeout(TimeoutDuration).WithRetry(3),
            CreateOptions(timeProvider));

        timeProvider.Advance(TimeoutDuration);

        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, attempts);
        var failure = Assert.IsType<Failure<string>>(outcome);
        Assert.IsType<TimeoutException>(failure.Exception);
    }

    [Fact]
    public async Task WithRetryThenWithTimeout_WhenRetriesExceedTimeout_FailsWithTimeoutException()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        var secondAttemptStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var attempts = 0;
        var flow = Flow.Create<string>(async cancellationToken =>
        {
            attempts++;
            if (attempts == 1)
                throw new InvalidOperationException("Attempt 1 failed");

            if (attempts == 2)
                secondAttemptStarted.TrySetResult();

            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("Unreachable");
        });

        // Act
        var execution = FlowEngine.ExecuteAsync(
            flow.WithRetry(5).WithTimeout(TimeoutDuration),
            CreateOptions(timeProvider));

        await secondAttemptStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        timeProvider.Advance(TimeoutDuration);

        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(attempts >= 2);
        var failure = Assert.IsType<Failure<string>>(outcome);
        Assert.IsType<TimeoutException>(failure.Exception);
    }

    [Fact]
    public async Task WithTimeoutInsideAny_PreservesExecutionTimeProvider()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        var flow = Flow.Any(CreateNeverCompletingFlow().WithTimeout(TimeoutDuration));

        // Act
        var execution = FlowEngine.ExecuteAsync(flow, CreateOptions(timeProvider));
        Assert.False(execution.IsCompleted);
        timeProvider.Advance(TimeoutDuration);
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        var failure = Assert.IsType<Failure<string>>(outcome);
        var aggregate = Assert.IsType<AggregateException>(failure.Exception);
        Assert.Contains(aggregate.InnerExceptions, exception => exception is TimeoutException);
    }

    [Fact]
    public async Task WithTimeoutInsideAll_PreservesExecutionTimeProvider()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var flow = Flow.All(CreateNeverCompletingFlow(started).WithTimeout(TimeoutDuration), Flow.Succeed("ready"));

        // Act
        var execution = FlowEngine.ExecuteAsync(flow, CreateOptions(timeProvider));

        await started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        timeProvider.Advance(TimeoutDuration);
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        var failure = Assert.IsType<Failure<string[]>>(outcome);
        var aggregate = Assert.IsType<AggregateException>(failure.Exception);
        Assert.Contains(aggregate.InnerExceptions, exception => exception is TimeoutException);
    }

    [Fact]
    public async Task WithTimeoutInChainReturnedFlow_PreservesExecutionTimeProvider()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var flow = Flow.Succeed("start").Chain(_ => CreateNeverCompletingFlow(started).WithTimeout(TimeoutDuration));

        // Act
        var execution = FlowEngine.ExecuteAsync(flow, CreateOptions(timeProvider));

        await started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        timeProvider.Advance(TimeoutDuration);
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        var failure = Assert.IsType<Failure<string>>(outcome);
        Assert.IsType<TimeoutException>(failure.Exception);
    }

    [Fact]
    public async Task WithTimeoutInRecoveredFlow_PreservesExecutionTimeProvider()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var flow = Flow.Fail<string>(new InvalidOperationException("trigger recovery"))
            .Recover(_ => CreateNeverCompletingFlow(started).WithTimeout(TimeoutDuration));

        // Act
        var execution = FlowEngine.ExecuteAsync(flow, CreateOptions(timeProvider));

        await started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        timeProvider.Advance(TimeoutDuration);
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        var failure = Assert.IsType<Failure<string>>(outcome);
        Assert.IsType<TimeoutException>(failure.Exception);
    }

    [Fact]
    public async Task WithTimeoutInResourceFlow_PreservesExecutionTimeProvider()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var flow = Flow.WithResource(
            acquire: () => new CancellationTokenSource(),
            use: _ => CreateNeverCompletingFlow(started).WithTimeout(TimeoutDuration));

        // Act
        var execution = FlowEngine.ExecuteAsync(flow, CreateOptions(timeProvider));

        await started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        timeProvider.Advance(TimeoutDuration);
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        var failure = Assert.IsType<Failure<string>>(outcome);
        Assert.IsType<TimeoutException>(failure.Exception);
    }

    [Fact]
    public async Task WithTimeout_OnResource_StartsAfterSynchronousAcquisitionReturns()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        var acquisitionStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var releaseAcquisition = new ManualResetEventSlim();
        var flow = Flow.WithResource(
            acquire: () =>
            {
                acquisitionStarted.TrySetResult();
                releaseAcquisition.Wait();
                return new CancellationTokenSource();
            },
            use: _ => Flow.Succeed(SimoneDeBeauvoir))
            .WithTimeout(TimeoutDuration);

        // Act
        var execution = Task.Run(() => FlowEngine.ExecuteAsync(flow, CreateOptions(timeProvider)));

        try
        {
            await acquisitionStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            timeProvider.Advance(TimeoutDuration);
        }
        finally
        {
            releaseAcquisition.Set();
        }

        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(Success(SimoneDeBeauvoir), outcome);
    }

    private static Execution.Options CreateOptions(TimeProvider timeProvider) =>
        new(CancellationToken.None) { TimeProvider = timeProvider };

    private static IFlow<string> CreateNeverCompletingFlow(TaskCompletionSource? started = null) =>
        Flow.Create<string>(async cancellationToken =>
        {
            started?.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return SimoneDeBeauvoir;
        });
}
