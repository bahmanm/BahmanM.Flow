using BahmanM.Flow.Description.Formatting;

namespace BahmanM.Flow.Description.Tests.Unit.Behaviours;

public class SyncChainBehaviourDescriptionTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Retry_Description_AroundSyncChainRetriesGeneratedChildOnly(bool returnDescription)
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
        Flow.Operations.Chain.Sync<int, int> operation = value =>
        {
            callbackExecutions++;
            Flow.Operations.Create.Sync<int> child = () =>
            {
                childAttempts++;
                if (childAttempts < 3)
                    throw new InvalidOperationException("try again");

                return value + 1;
            };
            return returnDescription
                ? (Flow.Create(() => child())).AsDescription()
                : Flow.Create(() => child());
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
        Assert.Contains("Chain.Sync<int, int>", descriptionText);
        Assert.IsType<Ast.Chain.Sync<int, int>>(lowered);
        Assert.Equal((0, 0, 0), countsAfterLowering);
        Assert.Equal(Outcome.Success(42), outcome);
        Assert.Equal(1, upstreamExecutions);
        Assert.Equal(1, callbackExecutions);
        Assert.Equal(3, childAttempts);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Timeout_Description_AroundSyncChainTimesGeneratedChild(bool returnDescription)
    {
        // Arrange
        var upstreamExecutions = 0;
        var callbackExecutions = 0;
        var childExecutions = 0;
        var childStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var childCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var releaseChild = new ManualResetEventSlim();
        var upstream = (Flow.Create(() =>
        {
            upstreamExecutions++;
            return 41;
        })).AsDescription();
        Flow.Operations.Chain.Sync<int, int> operation = value =>
        {
            callbackExecutions++;
            Flow.Operations.Create.Sync<int> child = () =>
            {
                childExecutions++;
                childStarted.TrySetResult();
                releaseChild.Wait(TimeSpan.FromSeconds(5));
                childCompleted.TrySetResult();
                return value + 1;
            };
            return returnDescription
                ? (Flow.Create(() => child())).AsDescription()
                : Flow.Create(() => child());
        };
        var chain = ((upstream).Chain(operation)).AsDescription();
        var description = ((chain).WithTimeout(TimeSpan.FromMilliseconds(200))).AsDescription();

        // Act
        var descriptionText = DescriptionFormatting.Describe(description);
        var lowered = description.Lower(FacadeBehaviourStrategyFactory.Instance);
        var countsAfterLowering = (upstreamExecutions, callbackExecutions, childExecutions);
        var execution = FlowEngine.ExecuteAsync(description);
        await childStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        var outcome = await execution;
        releaseChild.Set();
        await childCompleted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains("Timeout<int>", descriptionText);
        Assert.Contains("Chain.Sync<int, int>", descriptionText);
        Assert.IsType<Ast.Chain.CancellableAsync<int, int>>(lowered);
        Assert.Equal((0, 0, 0), countsAfterLowering);
        Assert.IsType<TimeoutException>(Assert.IsType<Failure<int>>(outcome).Exception);
        Assert.Equal(1, upstreamExecutions);
        Assert.Equal(1, callbackExecutions);
        Assert.Equal(1, childExecutions);
        Assert.True(childCompleted.Task.IsCompletedSuccessfully);
    }
}
