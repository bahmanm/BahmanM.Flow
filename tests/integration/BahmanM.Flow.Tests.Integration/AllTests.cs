using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow.Tests.Integration;

public class AllTests
{
    // Émilie du Châtelet (1706-1749) was a French natural philosopher and mathematician.
    // Her crowning achievement is considered to be her translation and commentary on Isaac Newton's work Principia Mathematica.
    private const string EmilieDuChatelet = "Émilie du Châtelet";

    // Brahmagupta (c. 598-c. 668 AD) was an Indian mathematician and astronomer.
    // He is the author of two early works on mathematics and astronomy: the Brāhmasphuṭasiddhānta and the Khaṇḍakhādyaka.
    private const string Brahmagupta = "Brahmagupta";

    [Fact]
    public async Task All_WhenAllFlowsSucceed_ReturnsSucceededOutcomeWithAllValues()
    {
        // Arrange
        var flow1 = Flow.Succeed(EmilieDuChatelet);
        var flow2 = Flow.Succeed(Brahmagupta);

        // Act
        var combinedFlow = Flow.All(flow1, flow2);
        var outcome = await FlowEngine.ExecuteAsync(combinedFlow);

        // Assert
        Assert.True(outcome.IsSuccess());
        var success = (Success<string[]>)outcome;
        Assert.Equal([EmilieDuChatelet, Brahmagupta], success.Value);
    }

    [Fact]
    public async Task All_WhenOneFlowFails_ReturnsFailedOutcomeWithAggregateException()
    {
        // Arrange
        var exception = new Exception("Something went wrong");
        var flow1 = Flow.Succeed(EmilieDuChatelet);
        var flow2 = Flow.Fail<string>(exception);

        // Act
        var combinedFlow = Flow.All(flow1, flow2);
        var outcome = await FlowEngine.ExecuteAsync(combinedFlow);

        // Assert
        Assert.True(outcome.IsFailure());
        var failure = (Failure<string[]>)outcome;
        var aggregateException = Assert.IsType<AggregateException>(failure.Exception);
        Assert.Single(aggregateException.InnerExceptions);
        Assert.Equal(exception, aggregateException.InnerExceptions[0]);
    }

    [Fact]
    public async Task All_WhenAllFlowsFail_ReturnsFailedOutcomeWithAggregateException()
    {
        // Arrange
        var exception1 = new Exception("First failure");
        var exception2 = new Exception("Second failure");
        var flow1 = Flow.Fail<string>(exception1);
        var flow2 = Flow.Fail<string>(exception2);

        // Act
        var combinedFlow = Flow.All(flow1, flow2);
        var outcome = await FlowEngine.ExecuteAsync(combinedFlow);

        // Assert
        Assert.True(outcome.IsFailure());
        var failure = (Failure<string[]>)outcome;
        var aggregateException = Assert.IsType<AggregateException>(failure.Exception);
        Assert.Equal(2, aggregateException.InnerExceptions.Count);
        Assert.Contains(exception1, aggregateException.InnerExceptions);
        Assert.Contains(exception2, aggregateException.InnerExceptions);
    }

    [Fact]
    public async Task All_WithMixedFlowTypes_ReturnsSucceededOutcome()
    {
        // Arrange
        // Liu Hui (c. 225–295 AD) was a Chinese mathematician who published a commentary in 263 AD on
        // Jiuzhang Suanshu (The Nine Chapters on the Mathematical Art).
        const string liuHui = "Liu Hui";
        var flow1 = Flow.Succeed(EmilieDuChatelet);
        var flow2 = Flow.Create(() => Brahmagupta);
        var flow3 = Flow.Create<string>(async () =>
        {
            await Task.Yield();
            return liuHui;
        });

        // Act
        var combinedFlow = Flow.All(flow1, flow2, flow3);
        var outcome = await FlowEngine.ExecuteAsync(combinedFlow);

        // Assert
        Assert.True(outcome.IsSuccess());
        var success = (Success<string[]>)outcome;
        Assert.Equal([EmilieDuChatelet, Brahmagupta, liuHui], success.Value);
    }

    [Fact]
    public async Task All_FollowedBySelect_TransformsTheArrayOfResults()
    {
        // Arrange
        var flow1 = Flow.Succeed("a");
        var flow2 = Flow.Succeed("b");
        var flow3 = Flow.Succeed("c");

        // Act
        var combinedAndSelected = Flow.All(flow1, flow2, flow3)
            .Select(results => string.Concat(results));

        var outcome = await FlowEngine.ExecuteAsync(combinedAndSelected);

        // Assert
        Assert.Equal(Success("abc"), outcome);
    }

    [Fact]
    public async Task WithRetry_OnFlowWithinAll_RetriesTheIndividualFlow()
    {
        // Arrange
        var attempts = 0;
        var flakyFlow = Flow.Create(() =>
        {
            attempts++;
            if (attempts < 3) throw new InvalidOperationException();
            return "flaky";
        }).WithRetry(3);

        var stableFlow = Flow.Succeed("stable");

        // Act
        var outcome = await FlowEngine.ExecuteAsync(Flow.All(flakyFlow, stableFlow));

        // Assert
        Assert.Equal(3, attempts);
        Assert.True(outcome.IsSuccess());
        var success = (Success<string[]>)outcome;
        Assert.Equal(["flaky", "stable"], success.Value);
    }
}
