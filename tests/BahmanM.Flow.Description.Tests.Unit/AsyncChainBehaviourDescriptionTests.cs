using BahmanM.Flow.Description.Serialisation;
using BahmanM.Flow.Description.Tests.Unit.Support;

namespace BahmanM.Flow.Description.Tests.Unit;

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
        var upstream = DescriptionFlowBuilder.Create(() =>
        {
            upstreamExecutions++;
            return 41;
        });
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
                ? DescriptionFlowBuilder.Create(child)
                : Flow.Create<int>(async () => await child());
        };
        var chain = DescriptionFlowBuilder.Chain(upstream, operation);
        var description = DescriptionFlowBuilder.Retry(chain, 3);

        // Act
        var descriptionText = DescriptionFormatter.Describe(description);
        var lowered = description.Lower();
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
        var upstream = DescriptionFlowBuilder.Create(() =>
        {
            upstreamExecutions++;
            return 41;
        });
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
                ? DescriptionFlowBuilder.Create(child)
                : Flow.Create<int>(async () => await child());
        };
        var chain = DescriptionFlowBuilder.Chain(upstream, operation);
        var description = DescriptionFlowBuilder.Timeout(chain, TimeSpan.FromMilliseconds(250));

        // Act
        var descriptionText = DescriptionFormatter.Describe(description);
        var lowered = description.Lower();
        var countsAfterLowering = (upstreamExecutions, callbackExecutions, childExecutions);
        var execution = FlowEngine.ExecuteAsync(description);
        await childStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Outcome<int> outcome;
        try
        {
            outcome = await execution.WaitAsync(TimeSpan.FromSeconds(2));
        }
        finally
        {
            releaseChild.TrySetResult();
        }
        await childCompleted.Task.WaitAsync(TimeSpan.FromSeconds(2));

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
