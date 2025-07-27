using BahmanM.Flow.Execution;
using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow.Tests.Unit;

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
        Operations.DoOnFailure.CancellableAsync onFailure = async (ex, token) =>
        {
            await Task.Delay(100, token);
            actionCalled = true;
            capturedException = ex;
        };


        var flow = Flow.Fail<string>(ZenosException).DoOnFailure(onFailure);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.True(actionCalled);
        Assert.Same(ZenosException, capturedException);
        Assert.Equal(Failure<string>(ZenosException), outcome);
    }

    [Fact]
    public async Task WhenActionThrows_ReturnsOriginalFailure()
    {
        // Arrange
        var actionException = new InvalidOperationException("Action failed!");
        Operations.DoOnFailure.Sync onFailure = _ => throw actionException;
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
        Operations.DoOnFailure.Async onFailure = async ex =>
        {
            await Task.Delay(1000);
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
        Operations.DoOnFailure.Async onFailure = _ => throw actionException;
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
        var options = new FlowExecutionOptions { CancellationToken = cts.Token };
        var actionCalled = false;
        var capturedException = null as Exception;

        Operations.DoOnFailure.CancellableAsync onFailure = async (ex, token) =>
        {
            await Task.Delay(1000, token);
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
