
namespace BahmanM.Flow.Description.Tests.Unit;

public class AllDescriptionTests
{
    [Fact]
    public async Task All_Description_Preserves_Flow_Order_And_Lowers_To_Equivalent_Ast()
    {
        // Arrange
        var description = (Flow.All((Flow.Succeed(2)).AsDescription(), (Flow.Succeed(1)).AsDescription())).AsDescription();
        var expectedFlow = Flow.All(Flow.Succeed(2), Flow.Succeed(1));

        // Act
        var actual = await FlowEngine.ExecuteAsync(description);
        var expected = await FlowEngine.ExecuteAsync(expectedFlow);

        // Assert
        Assert.Equal(
            Assert.IsType<Success<int[]>>(expected).Value,
            Assert.IsType<Success<int[]>>(actual).Value);
        Assert.Equal(new[] { 2, 1 }, Assert.IsType<Success<int[]>>(actual).Value);

        var lowered = Assert.IsType<Ast.Primitive.All<int>>(description.Lower(FacadeBehaviourStrategyFactory.Instance));
        Assert.Equal(new[] { 2, 1 }, lowered.Flows
            .Select(flow => Assert.IsType<Ast.Primitive.Succeed<int>>(flow).Value));
    }

    [Fact]
    public async Task All_Description_Starts_All_Flows_Concurrently_And_Defers_Operations()
    {
        // Arrange
        var allStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var startedCount = 0;

        Flow.Operations.Create.Async<int> CreateOperation(int value) => async () =>
        {
            if (Interlocked.Increment(ref startedCount) == 2)
                allStarted.TrySetResult(true);

            await release.Task;
            return value;
        };

        var description = (Flow.All((Flow.Create(() => CreateOperation(1)())).AsDescription(), (Flow.Create(() => CreateOperation(2)())).AsDescription())).AsDescription();

        // Act
        Assert.Equal(0, startedCount);
        var execution = FlowEngine.ExecuteAsync(description);
        try
        {
            await allStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            release.TrySetResult(true);
        }

        var actual = await execution;

        // Assert
        Assert.Equal(2, startedCount);
        Assert.Equal(new[] { 1, 2 }, Assert.IsType<Success<int[]>>(actual).Value);
    }

    [Fact]
    public async Task All_Description_Aggregates_Failures_And_Waits_For_Every_Flow()
    {
        // Arrange
        var firstException = new InvalidOperationException("first failure");
        var secondException = new ArgumentException("second failure");
        var releaseSecond = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Flow.Operations.Create.Async<int> secondFlow = async () =>
        {
            await releaseSecond.Task;
            throw secondException;
        };
        var description = (Flow.All((Flow.Fail<int>(firstException)).AsDescription(), (Flow.Create(() => secondFlow())).AsDescription())).AsDescription();

        // Act
        var execution = FlowEngine.ExecuteAsync(description);
        Assert.False(execution.IsCompleted);
        releaseSecond.TrySetResult(true);
        var actual = await execution;

        // Assert
        var aggregate = Assert.IsType<AggregateException>(Assert.IsType<Failure<int[]>>(actual).Exception);
        Assert.Collection(aggregate.InnerExceptions,
            exception => Assert.Same(firstException, exception),
            exception => Assert.Same(secondException, exception));
    }
}
