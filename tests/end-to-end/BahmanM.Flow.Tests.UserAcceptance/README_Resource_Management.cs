/*
using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow.Tests.UserAcceptance;

public class README_Resource_Management
{
    [Fact]
    [Trait("Category", "Acceptance")]
    public async Task Should_Succeed_And_Dispose_Resource_When_Operation_Succeeds()
    {
        // Arrange
        var resource = new DisposableResource();
        var resourceFlow = Flow.WithResource(
            acquire: () => resource,
            use: res => Flow.Succeed(res.GetValue())
        );

        // Act
        var outcome = await FlowEngine.ExecuteAsync(resourceFlow);

        // Assert
        Assert.Equal(Success("Success from resource!"), outcome);
        Assert.True(resource.IsDisposed);
    }

    [Fact]
    [Trait("Category", "Acceptance")]
    public async Task Should_Fail_And_Still_Dispose_Resource_When_Operation_Fails()
    {
        // Arrange
        var resource = new DisposableResource();
        var exception = new InvalidOperationException("Operation failed!");
        var resourceFlow = Flow.WithResource(
            acquire: () => resource,
            use: res => Flow.Fail<string>(exception)
        );

        // Act
        var outcome = await FlowEngine.ExecuteAsync(resourceFlow);

        // Assert
        Assert.Equal(Failure<string>(exception), outcome);
        Assert.True(resource.IsDisposed);
    }

    [Fact]
    [Trait("Category", "Acceptance")]
    public async Task Chain_WithResource_Should_Succeed_And_Dispose_When_Operation_Succeeds()
    {
        // Arrange
        var resource = new DisposableResource();
        var chainedFlow = Flow.Succeed("input")
            .Chain(_ => Flow.WithResource(
                acquire: () => resource,
                use: res => Flow.Succeed(res.GetValue())
            ));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(chainedFlow);

        // Assert
        Assert.Equal(Success("Success from resource!"), outcome);
        Assert.True(resource.IsDisposed);
    }

    [Fact]
    [Trait("Category", "Acceptance")]
    public async Task Chain_WithResource_Should_Fail_And_Dispose_When_Operation_Fails()
    {
        // Arrange
        var resource = new DisposableResource();
        var exception = new InvalidOperationException("Operation failed!");
        var chainedFlow = Flow.Succeed("input")
            .Chain(_ => Flow.WithResource(
                acquire: () => resource,
                use: res => Flow.Fail<string>(exception)
            ));

        // Act
        var outcome = await FlowEngine.ExecuteAsync(chainedFlow);

        // Assert
        Assert.Equal(Failure<string>(exception), outcome);
        Assert.True(resource.IsDisposed);
    }
}

#region Mocks and Stubs

// This mock resource tracks its disposal status, allowing us to verify the behavior of WithResource.
internal class DisposableResource : IDisposable
{
    public bool IsDisposed { get; private set; }

    public string GetValue()
    {
        if (IsDisposed)
        {
            throw new ObjectDisposedException(nameof(DisposableResource));
        }
        return "Success from resource!";
    }

    public void Dispose()
    {
        IsDisposed = true;
    }
}

#endregion
*/
