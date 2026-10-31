using BahmanM.Flow.Tests.Support;
using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow.Tests.Integration.Core;

public class WithRetryWithResourceTests
{
    [Fact]
    public async Task WithRetry_AppliedToResourceScope_DoesNotRetryOrReacquire()
    {
        var acquisitionCount = 0;
        var operationAttempts = 0;
        var resource = new DisposableProbe();
        var exception = new InvalidOperationException("Operation failed.");

        var flow = Flow.WithResource(
            acquire: () =>
            {
                acquisitionCount++;
                return resource;
            },
            use: _ => Flow.Create((Func<int>)(() =>
            {
                operationAttempts++;
                throw exception;
            })))
            .WithRetry(3);

        var outcome = await FlowEngine.ExecuteAsync(flow);

        Assert.Equal(Failure<int>(exception), outcome);
        Assert.Equal(1, acquisitionCount);
        Assert.Equal(1, operationAttempts);
        Assert.True(resource.IsDisposed);
    }

    [Fact]
    public async Task WithRetry_AppliedInsideResourceScope_RetriesOperationUsingSameResource()
    {
        var acquisitionCount = 0;
        var operationAttempts = 0;
        var resource = new DisposableProbe();

        var flow = Flow.WithResource(
            acquire: () =>
            {
                acquisitionCount++;
                return resource;
            },
            use: usedResource => Flow.Create(() =>
            {
                Assert.Same(resource, usedResource);
                operationAttempts++;
                if (operationAttempts < 3)
                {
                    throw new InvalidOperationException("Temporary failure.");
                }

                return 42;
            }).WithRetry(3));

        var outcome = await FlowEngine.ExecuteAsync(flow);

        Assert.Equal(Success(42), outcome);
        Assert.Equal(1, acquisitionCount);
        Assert.Equal(3, operationAttempts);
        Assert.True(resource.IsDisposed);
    }
}
