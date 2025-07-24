using System;
using System.Threading.Tasks;
using BahmanM.Flow;
using Xunit;
using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow.Tests.Unit;

public class DoOnFailureTests
{
    // Zeno of Citium (c. 334 – c. 262 BC) was a Hellenistic philosopher
    // from Citium, Cyprus. Zeno was the founder of the Stoic school of philosophy.
    private static readonly Exception ZenosException = new InvalidOperationException("Zeno's Paradox");

    [Fact]
    public async Task WhenFlowFails_CallsActionAndReturnsOriginalFailure()
    {
        // Arrange
        var actionCalled = false;
        Exception? capturedException = null;
        Action<Exception> onFailure = ex =>
        {
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
    public async Task WhenFlowSucceeds_DoesNotCallActionAndReturnsOriginalSuccess()
    {
        // Arrange
        var actionCalled = false;
        Action<Exception> onFailure = _ => actionCalled = true;
        var flow = Flow.Succeed("Success").DoOnFailure(onFailure);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.False(actionCalled);
        Assert.Equal(Success("Success"), outcome);
    }

    [Fact]
    public async Task WhenActionThrows_ReturnsOriginalFailure()
    {
        // Arrange
        var actionException = new InvalidOperationException("Action failed!");
        Action<Exception> onFailure = _ => throw actionException;
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
        Exception? capturedException = null;
        Func<Exception, Task> onFailure = async ex =>
        {
            await Task.Delay(1);
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
    public async Task WhenFlowSucceeds_DoesNotCallAsyncActionAndReturnsOriginalSuccess()
    {
        // Arrange
        var actionCalled = false;
        Func<Exception, Task> onFailure = async _ =>
        {
            await Task.Delay(1);
            actionCalled = true;
        };
        var flow = Flow.Succeed("Success").DoOnFailure(onFailure);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.False(actionCalled);
        Assert.Equal(Success("Success"), outcome);
    }

    [Fact]
    public async Task WhenAsyncActionThrows_ReturnsOriginalFailure()
    {
        // Arrange
        var actionException = new InvalidOperationException("Action failed!");
        Func<Exception, Task> onFailure = _ => throw actionException;
        var flow = Flow.Fail<string>(ZenosException).DoOnFailure(onFailure);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Failure<string>(ZenosException), outcome);
    }
}
