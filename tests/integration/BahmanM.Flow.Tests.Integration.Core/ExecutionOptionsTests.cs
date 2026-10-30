namespace BahmanM.Flow.Tests.Integration.Core;

public class ExecutionOptionsTests
{
    [Fact]
    public void Constructor_DefaultsTimeProviderToSystem()
    {
        var options = new Execution.Options(CancellationToken.None);

        Assert.Same(TimeProvider.System, options.TimeProvider);
    }

    [Fact]
    public void TimeProvider_WhenSetToNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Execution.Options(CancellationToken.None) { TimeProvider = null! });
    }
}
