using BahmanM.Flow.Description.Formatting;

namespace BahmanM.Flow.Description.Tests.Unit.Behaviours;

public class AsyncChainBehaviourDescriptionTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Retry_Description_AroundAsyncChainRetriesGeneratedChildOnly(bool returnDescription)
    {
        // Arrange
        var upstreamExecutions = 0;
        var callbackExecutions = 0;
        var childAttempts = 0;
        var upstream = (Flow.Create(() =>
        {
            upstreamExecutions++;
            return 41;
        })).AsDescription();
        Flow.Operations.Chain.Async<int, int> operation = async value =>
        {
            callbackExecutions++;
            await Task.Yield();
            Flow.Operations.Create.Async<int> child = async () =>
            {
                childAttempts++;
                await Task.Yield();
                if (childAttempts < 3)
                    throw new InvalidOperationException("try again");

                return value + 1;
            };
            return returnDescription
                ? (Flow.Create(() => child())).AsDescription()
                : Flow.Create(async () => await child());
        };
        var chain = ((upstream).Chain(operation)).AsDescription();
        var description = ((chain).WithRetry(3)).AsDescription();

        // Act
        var descriptionText = DescriptionFormatting.Describe(description);
        var lowered = description.Lower(FacadeBehaviourStrategyFactory.Instance);
        var countsAfterLowering = (upstreamExecutions, callbackExecutions, childAttempts);
        var outcome = await FlowEngine.ExecuteAsync(description);

        // Assert
        Assert.Contains("Retry<int>", descriptionText);
        Assert.Contains("Chain.Async<int, int>", descriptionText);
        Assert.IsType<Ast.Chain.Async<int, int>>(lowered);
        Assert.Equal((0, 0, 0), countsAfterLowering);
        Assert.Equal(Outcome.Success(42), outcome);
        Assert.Equal(1, upstreamExecutions);
        Assert.Equal(1, callbackExecutions);
        Assert.Equal(3, childAttempts);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Timeout_Description_AroundAsyncChainTimesGeneratedChild(bool returnDescription)
    {
        // Arrange
        var upstreamExecutions = 0;
        var callbackExecutions = 0;
        var childExecutions = 0;
        var childStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var childCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseChild = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var upstream = (Flow.Create(() =>
        {
            upstreamExecutions++;
            return 41;
        })).AsDescription();
        Flow.Operations.Chain.Async<int, int> operation = async value =>
        {
            callbackExecutions++;
            await Task.Yield();
            Flow.Operations.Create.Async<int> child = async () =>
            {
                childExecutions++;
                childStarted.TrySetResult();
                await releaseChild.Task;
                childCompleted.TrySetResult();
                return value + 1;
            };
            return returnDescription
                ? (Flow.Create(() => child())).AsDescription()
                : Flow.Create(async () => await child());
        };
        var chain = ((upstream).Chain(operation)).AsDescription();
        var description = ((chain).WithTimeout(TimeSpan.FromMilliseconds(500))).AsDescription();

        // Act
        var descriptionText = DescriptionFormatting.Describe(description);
        var lowered = description.Lower(FacadeBehaviourStrategyFactory.Instance);
        var countsAfterLowering = (upstreamExecutions, callbackExecutions, childExecutions);
        var execution = FlowEngine.ExecuteAsync(description);
        await childStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        Outcome<int> outcome;
        try
        {
            outcome = await execution.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        }
        finally
        {
            releaseChild.TrySetResult();
        }
        await childCompleted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains("Timeout<int>", descriptionText);
        Assert.Contains("Chain.Async<int, int>", descriptionText);
        Assert.IsType<Ast.Chain.CancellableAsync<int, int>>(lowered);
        Assert.Equal((0, 0, 0), countsAfterLowering);
        Assert.IsType<TimeoutException>(Assert.IsType<Failure<int>>(outcome).Exception);
        Assert.Equal(1, upstreamExecutions);
        Assert.Equal(1, callbackExecutions);
        Assert.Equal(1, childExecutions);
        Assert.True(childCompleted.Task.IsCompletedSuccessfully);
    }
}
