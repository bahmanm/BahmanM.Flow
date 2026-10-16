using BahmanM.Flow.Description.Rendering;

namespace BahmanM.Flow.Description.Tests.Unit.Behaviours;

public class CancellableAsyncChainBehaviourDescriptionTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Retry_Description_AroundCancellableAsyncChainRetriesGeneratedChildOnly(bool returnDescription)
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var upstreamExecutions = 0;
        var callbackExecutions = 0;
        var childAttempts = 0;
        var callbackToken = CancellationToken.None;
        var childTokens = new List<CancellationToken>();
        var upstream = (Flow.Create(() =>
        {
            upstreamExecutions++;
            return 41;
        })).AsDescription();
        Flow.Operations.Chain.CancellableAsync<int, int> operation = async (value, token) =>
        {
            callbackExecutions++;
            callbackToken = token;
            await Task.Yield();
            Flow.Operations.Create.CancellableAsync<int> child = async childToken =>
            {
                childAttempts++;
                childTokens.Add(childToken);
                await Task.Yield();
                if (childAttempts < 3)
                    throw new InvalidOperationException("try again");

                return value + 1;
            };
            return returnDescription
                ? (Flow.Create(cancellationToken => child(cancellationToken))).AsDescription()
                : Flow.Create(cancellationToken => child(cancellationToken));
        };
        var chain = ((upstream).Chain(operation)).AsDescription();
        var description = ((chain).WithRetry(3)).AsDescription();
        var options = new Execution.Options(cancellation.Token);

        // Act
        var descriptionText = Renderer.Describe(description);
        var lowered = description.Lower(FacadeBehaviourStrategyFactory.Instance);
        var countsAfterLowering = (upstreamExecutions, callbackExecutions, childAttempts);
        var outcome = await FlowEngine.ExecuteAsync(description, options);

        // Assert
        Assert.Contains("Retry<int>", descriptionText);
        Assert.Contains("Chain.CancellableAsync<int, int>", descriptionText);
        Assert.IsType<Ast.Chain.CancellableAsync<int, int>>(lowered);
        Assert.Equal((0, 0, 0), countsAfterLowering);
        Assert.Equal(Outcome.Success(42), outcome);
        Assert.Equal(cancellation.Token, callbackToken);
        Assert.Equal(1, upstreamExecutions);
        Assert.Equal(1, callbackExecutions);
        Assert.Equal(3, childAttempts);
        Assert.All(childTokens, token => Assert.Equal(cancellation.Token, token));
    }

    [Fact]
    public async Task Timeout_Description_AroundCancellableAsyncChainCancelsBindCallback()
    {
        // Arrange
        var callbackExecutions = 0;
        var callbackStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callbackToken = CancellationToken.None;
        Flow.Operations.Chain.CancellableAsync<int, int> operation = async (_, token) =>
        {
            callbackExecutions++;
            callbackToken = token;
            callbackStarted.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return (Flow.Succeed(42)).AsDescription();
        };
        var description = (((((Flow.Succeed(1)).AsDescription()).Chain(operation)).AsDescription()).WithTimeout(TimeSpan.FromMilliseconds(300))).AsDescription();

        // Act
        var descriptionText = Renderer.Describe(description);
        var lowered = description.Lower(FacadeBehaviourStrategyFactory.Instance);
        var callbackExecutionsAfterLowering = callbackExecutions;
        var execution = FlowEngine.ExecuteAsync(description);
        await callbackStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains("Timeout<int>", descriptionText);
        Assert.Contains("Chain.CancellableAsync<int, int>", descriptionText);
        Assert.IsType<Ast.Chain.CancellableAsync<int, int>>(lowered);
        Assert.Equal(0, callbackExecutionsAfterLowering);
        Assert.IsType<TimeoutException>(Assert.IsType<Failure<int>>(outcome).Exception);
        Assert.Equal(1, callbackExecutions);
        Assert.True(callbackToken.IsCancellationRequested);
    }

    [Fact]
    public async Task Timeout_Description_AroundCancellableAsyncChainPropagatesExternalCancellationToBindCallback()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var callbackStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callbackToken = CancellationToken.None;
        Flow.Operations.Chain.CancellableAsync<int, int> operation = async (_, token) =>
        {
            callbackToken = token;
            callbackStarted.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return (Flow.Succeed(42)).AsDescription();
        };
        var chain = (((Flow.Succeed(1)).AsDescription()).Chain(operation)).AsDescription();
        var description = ((chain).WithTimeout(TimeSpan.FromSeconds(5))).AsDescription();

        // Act
        var execution = FlowEngine.ExecuteAsync(
            description,
            new Execution.Options(cancellation.Token));
        await callbackStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        await cancellation.CancelAsync();
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        // Assert
        Assert.IsAssignableFrom<OperationCanceledException>(Assert.IsType<Failure<int>>(outcome).Exception);
        Assert.True(callbackToken.IsCancellationRequested);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Timeout_Description_AroundCancellableAsyncChainCancelsGeneratedChild(bool returnDescription)
    {
        // Arrange
        var upstreamExecutions = 0;
        var callbackExecutions = 0;
        var childExecutions = 0;
        var childStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var childToken = CancellationToken.None;
        var upstream = (Flow.Create(() =>
        {
            upstreamExecutions++;
            return 41;
        })).AsDescription();
        Flow.Operations.Chain.CancellableAsync<int, int> operation = async (value, _) =>
        {
            callbackExecutions++;
            await Task.Yield();
            Flow.Operations.Create.CancellableAsync<int> child = async token =>
            {
                childExecutions++;
                childToken = token;
                childStarted.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return value + 1;
            };
            return returnDescription
                ? (Flow.Create(cancellationToken => child(cancellationToken))).AsDescription()
                : Flow.Create(token => child(token));
        };
        var chain = ((upstream).Chain(operation)).AsDescription();
        var description = ((chain).WithTimeout(TimeSpan.FromMilliseconds(400))).AsDescription();

        // Act
        var descriptionText = Renderer.Describe(description);
        var lowered = description.Lower(FacadeBehaviourStrategyFactory.Instance);
        var countsAfterLowering = (upstreamExecutions, callbackExecutions, childExecutions);
        var execution = FlowEngine.ExecuteAsync(description);
        await childStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains("Timeout<int>", descriptionText);
        Assert.Contains("Chain.CancellableAsync<int, int>", descriptionText);
        Assert.IsType<Ast.Chain.CancellableAsync<int, int>>(lowered);
        Assert.Equal((0, 0, 0), countsAfterLowering);
        Assert.IsType<TimeoutException>(Assert.IsType<Failure<int>>(outcome).Exception);
        Assert.Equal(1, upstreamExecutions);
        Assert.Equal(1, callbackExecutions);
        Assert.Equal(1, childExecutions);
        Assert.True(childToken.IsCancellationRequested);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Timeout_Description_AroundCancellableAsyncChainPropagatesExternalCancellationToGeneratedChild(
        bool returnDescription)
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var childStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var childToken = CancellationToken.None;
        Flow.Operations.Chain.CancellableAsync<int, int> operation = async (value, _) =>
        {
            await Task.Yield();
            Flow.Operations.Create.CancellableAsync<int> child = async token =>
            {
                childToken = token;
                childStarted.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return value + 1;
            };
            return returnDescription
                ? (Flow.Create(cancellationToken => child(cancellationToken))).AsDescription()
                : Flow.Create(token => child(token));
        };
        var chain = (((Flow.Succeed(41)).AsDescription()).Chain(operation)).AsDescription();
        var description = ((chain).WithTimeout(TimeSpan.FromSeconds(5))).AsDescription();

        // Act
        var execution = FlowEngine.ExecuteAsync(
            description,
            new Execution.Options(cancellation.Token));
        await childStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        await cancellation.CancelAsync();
        var outcome = await execution.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        // Assert
        Assert.IsAssignableFrom<OperationCanceledException>(Assert.IsType<Failure<int>>(outcome).Exception);
        Assert.True(childToken.IsCancellationRequested);
    }
}
