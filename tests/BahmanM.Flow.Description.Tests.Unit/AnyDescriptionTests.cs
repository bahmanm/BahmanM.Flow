using BahmanM.Flow.Description.Tests.Unit.Support;

namespace BahmanM.Flow.Description.Tests.Unit;

public class AnyDescriptionTests
{
    [Fact]
    public void Any_Description_Lowers_Children_In_Input_Order()
    {
        // Arrange
        var description = DescriptionFlowBuilder.Any(
            DescriptionFlowBuilder.Succeed(2),
            DescriptionFlowBuilder.Succeed(1));

        // Act
        var lowered = Assert.IsType<Ast.Primitive.Any<int>>(description.Lower());

        // Assert
        Assert.Equal([2, 1], lowered.Flows
            .Select(flow => Assert.IsType<Ast.Primitive.Succeed<int>>(flow).Value));
    }

    [Fact]
    public async Task Any_Description_Returns_First_Success_And_Cancels_Loser()
    {
        // Arrange
        var bothStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseWinner = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var loserCancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var startedCount = 0;

        void MarkStarted()
        {
            if (Interlocked.Increment(ref startedCount) == 2)
                bothStarted.TrySetResult(true);
        }

        Flow.Operations.Create.CancellableAsync<int> winner = async _ =>
        {
            MarkStarted();
            await releaseWinner.Task;
            return 42;
        };
        Flow.Operations.Create.CancellableAsync<int> loser = async cancellationToken =>
        {
            MarkStarted();
            try
            {
                await Task.Delay(global::System.Threading.Timeout.InfiniteTimeSpan, cancellationToken);
                return -1;
            }
            catch (TaskCanceledException)
            {
                loserCancelled.TrySetResult(true);
                throw;
            }
        };
        var description = DescriptionFlowBuilder.Any(
            DescriptionFlowBuilder.Create(winner),
            DescriptionFlowBuilder.Create(loser));
        var expectedFlow = Flow.Any(
            Flow.Create(cancellationToken => winner(cancellationToken)),
            Flow.Create(cancellationToken => loser(cancellationToken)));

        // Act
        Assert.Equal(0, startedCount);
        var execution = FlowEngine.ExecuteAsync(description);
        try
        {
            await bothStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            releaseWinner.TrySetResult(true);
        }

        var actual = await execution;
        await loserCancelled.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var expected = await FlowEngine.ExecuteAsync(expectedFlow);

        // Assert
        Assert.Equal(
            Assert.IsType<Success<int>>(expected).Value,
            Assert.IsType<Success<int>>(actual).Value);
        Assert.Equal(42, Assert.IsType<Success<int>>(actual).Value);
        Assert.IsType<Ast.Primitive.Any<int>>(description.Lower());
    }

    [Fact]
    public async Task Any_Description_Aggregates_Failures_When_All_Flows_Fail()
    {
        // Arrange
        var firstException = new InvalidOperationException("first failure");
        var secondException = new ArgumentException("second failure");
        var description = DescriptionFlowBuilder.Any(
            DescriptionFlowBuilder.Fail<int>(firstException),
            DescriptionFlowBuilder.Fail<int>(secondException));

        // Act
        var actual = await FlowEngine.ExecuteAsync(description);

        // Assert
        var aggregate = Assert.IsType<AggregateException>(Assert.IsType<Failure<int>>(actual).Exception);
        Assert.Equal(2, aggregate.InnerExceptions.Count);
        Assert.Contains(aggregate.InnerExceptions, exception => ReferenceEquals(firstException, exception));
        Assert.Contains(aggregate.InnerExceptions, exception => ReferenceEquals(secondException, exception));
    }
}
