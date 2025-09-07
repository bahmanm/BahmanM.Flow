using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow.Tests.Integration;

public class DoOnFailureTests
{
    // Zeno of Citium (c. 334 – c. 262 BC) was a Hellenistic philosopher
    // from Citium, Cyprus. Zeno was the founder of the Stoic school of philosophy.
    private static readonly Exception ZenosException = new InvalidOperationException("Zeno's Paradox");

    [Fact]
    public async Task WhenFlowFails_CallsCancellableAsyncActionAndReturnsOriginalFailure()
    {
        // Arrange
        var actionCalled = false;
        Exception? capturedException = null;
        Flow.Operations.DoOnFailure.CancellableAsync onFailure = async (ex, token) =>
        {
            await Task.Yield();
            actionCalled = true;
            capturedException = ex;
        };

        var flow = Flow.Fail<string>(ZenosException).DoOnFailure(onFailure);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.True(actionCalled);
        Assert.Equal(ZenosException, capturedException);
        Assert.Equal(Failure<string>(ZenosException), outcome);
    }

    [Fact]
    public async Task WhenActionThrows_ReturnsOriginalFailure()
    {
        // Arrange
        var actionException = new InvalidOperationException("Action failed!");
        Flow.Operations.DoOnFailure.Sync onFailure = _ => throw actionException;
        var flow = Flow.Fail<string>(ZenosException).DoOnFailure(onFailure);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Failure<string>(ZenosException), outcome);
    }

    [Fact]
    public async Task WhenFlowFails_CallsAsyncActionAndReturnsOriginalFailure()
    {
        // Arrange
        var actionCalled = false;
        var capturedException = null as Exception;
        Flow.Operations.DoOnFailure.Async onFailure = async ex =>
        {
            await Task.Yield();
            actionCalled = true;
            capturedException = ex;
        };

        var flow = Flow.Fail<string>(ZenosException).DoOnFailure(onFailure);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.True(actionCalled);
        Assert.Equal(Failure<string>(ZenosException), outcome);
        Assert.Equal(ZenosException, capturedException);
    }

    [Fact]
    public async Task WhenAsyncActionThrows_ReturnsOriginalFailure()
    {
        // Arrange
        var actionException = new InvalidOperationException("Action failed!");
        Flow.Operations.DoOnFailure.Async onFailure = _ => throw actionException;
        var flow = Flow.Fail<string>(ZenosException).DoOnFailure(onFailure);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Failure<string>(ZenosException), outcome);
    }

    [Fact]
    public async Task WhenCancellableAsyncActionIsCancelled_ReturnsOriginalFailure()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var options = new Execution.Options(CancellationToken: cts.Token);
        var actionCalled = false;
        var capturedException = null as Exception;

        Flow.Operations.DoOnFailure.CancellableAsync onFailure = async (ex, token) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            actionCalled = true;
            capturedException = ex;  // Unreachable because of cancellation
        };

        var flow = Flow.Fail<string>(ZenosException).DoOnFailure(onFailure);

        // Act
        await cts.CancelAsync();
        var outcome = await FlowEngine.ExecuteAsync(flow, options);

        // Assert
        Assert.False(actionCalled);
        Assert.Equal(Failure<string>(ZenosException), outcome);
        Assert.Null(capturedException);

        // Clean up
        cts.Dispose();
    }
}
