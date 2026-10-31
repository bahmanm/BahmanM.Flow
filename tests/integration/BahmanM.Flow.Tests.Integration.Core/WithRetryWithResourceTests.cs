using BahmanM.Flow.Tests.Support;
using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow.Tests.Integration.Core;

public class WithRetryWithResourceTests
{
    [Fact]
    public async Task WithRetry_AppliedToResourceScope_DoesNotRetryOrReacquire()
    {
        // Arrange
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

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Failure<int>(exception), outcome);
        Assert.Equal(1, acquisitionCount);
        Assert.Equal(1, operationAttempts);
        Assert.True(resource.IsDisposed);
    }

    [Fact]
    public async Task WithRetry_AppliedInsideResourceScope_RetriesOperationUsingSameResource()
    {
        // Arrange
        var acquisitionCount = 0;
        var operationAttempts = 0;
        var resource = new DisposableProbe();
        var resourcesUsed = new List<DisposableProbe>();

        var flow = Flow.WithResource(
            acquire: () =>
            {
                acquisitionCount++;
                return resource;
            },
            use: usedResource => Flow.Create(() =>
            {
                resourcesUsed.Add(usedResource);
                operationAttempts++;
                if (operationAttempts < 3)
                {
                    throw new InvalidOperationException("Temporary failure.");
                }

                return 42;
            }).WithRetry(3));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Success(42), outcome);
        Assert.Equal(1, acquisitionCount);
        Assert.Equal(3, operationAttempts);
        Assert.All(resourcesUsed, usedResource => Assert.Same(resource, usedResource));
        Assert.True(resource.IsDisposed);
    }

    [Fact]
    public async Task WithRetry_AppliedToAsyncResourceScope_DoesNotRetryOrReacquire()
    {
        // Arrange
        var acquisitionCount = 0;
        var operationAttempts = 0;
        var resource = new AsyncDisposableProbe();
        var exception = new InvalidOperationException("Async resource operation failed.");
        var flow = Flow.WithResource(
            acquireAsync: () =>
            {
                acquisitionCount++;
                return Task.FromResult(resource);
            },
            use: _ => Flow.Create<int>((Func<int>)(() =>
            {
                operationAttempts++;
                throw exception;
            })))
            .WithRetry(3);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Failure<int>(exception), outcome);
        Assert.Equal(1, acquisitionCount);
        Assert.Equal(1, operationAttempts);
        Assert.True(resource.IsDisposed);
    }

    [Fact]
    public async Task WithRetry_AppliedInsideAsyncResourceScope_RetriesOperationUsingSameResource()
    {
        // Arrange
        var acquisitionCount = 0;
        var operationAttempts = 0;
        var resource = new AsyncDisposableProbe();
        var resourcesUsed = new List<AsyncDisposableProbe>();
        var flow = Flow.WithResource(
            acquireAsync: () =>
            {
                acquisitionCount++;
                return Task.FromResult(resource);
            },
            use: usedResource => Flow.Create<int>(() =>
            {
                resourcesUsed.Add(usedResource);
                operationAttempts++;
                if (operationAttempts < 3)
                {
                    throw new InvalidOperationException("Temporary async resource failure.");
                }

                return 42;
            }).WithRetry(3));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Success(42), outcome);
        Assert.Equal(1, acquisitionCount);
        Assert.Equal(3, operationAttempts);
        Assert.All(resourcesUsed, usedResource => Assert.Same(resource, usedResource));
        Assert.True(resource.IsDisposed);
    }

    [Fact]
    public async Task WithRetry_AppliedToCancellableAsyncResourceScope_DoesNotRetryOrReacquire()
    {
        // Arrange
        var acquisitionCount = 0;
        var operationAttempts = 0;
        var resource = new AsyncDisposableProbe();
        var exception = new InvalidOperationException("Cancellable async resource operation failed.");
        var flow = Flow.WithResource<AsyncDisposableProbe, int>(
            acquireAsync: _ =>
            {
                acquisitionCount++;
                return Task.FromResult(resource);
            },
            use: _ => Flow.Create<int>((Func<int>)(() =>
            {
                operationAttempts++;
                throw exception;
            })))
            .WithRetry(3);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Failure<int>(exception), outcome);
        Assert.Equal(1, acquisitionCount);
        Assert.Equal(1, operationAttempts);
        Assert.True(resource.IsDisposed);
    }

    [Fact]
    public async Task WithRetry_AppliedInsideCancellableAsyncResourceScope_RetriesOperationUsingSameResource()
    {
        // Arrange
        var acquisitionCount = 0;
        var operationAttempts = 0;
        var resource = new AsyncDisposableProbe();
        var resourcesUsed = new List<AsyncDisposableProbe>();
        var flow = Flow.WithResource<AsyncDisposableProbe, int>(
            acquireAsync: _ =>
            {
                acquisitionCount++;
                return Task.FromResult(resource);
            },
            use: usedResource => Flow.Create<int>(() =>
            {
                resourcesUsed.Add(usedResource);
                operationAttempts++;
                if (operationAttempts < 3)
                {
                    throw new InvalidOperationException("Temporary cancellable async resource failure.");
                }

                return 42;
            }).WithRetry(3));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(flow);

        // Assert
        Assert.Equal(Success(42), outcome);
        Assert.Equal(1, acquisitionCount);
        Assert.Equal(3, operationAttempts);
        Assert.All(resourcesUsed, usedResource => Assert.Same(resource, usedResource));
        Assert.True(resource.IsDisposed);
    }
}
