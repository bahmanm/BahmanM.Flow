using BahmanM.Flow.Description.Serialisation;
using BahmanM.Flow.Description.Tests.Unit.Support;

namespace BahmanM.Flow.Description.Tests.Unit;

public class DescriptionSerialisationTests
{
    [Fact]
    public void Describe_Shows_Structure_And_Callback_Identity_Without_Invoking_Callback()
    {
        // Arrange
        var invocationCount = 0;
        Flow.Operations.Select.Sync<string, int> operation = value =>
        {
            invocationCount++;
            return value.Length;
        };
        var description = DescriptionFlowBuilder.Select(DescriptionFlowBuilder.Succeed("Hello"), operation);

        // Act
        var result = DescriptionFormatter.Describe(description);

        // Assert
        Assert.Contains("Select.Sync<string, int>", result);
        Assert.Contains("operation: lambda(string) -> int", result);
        Assert.Contains("Succeed<string>", result);
        Assert.Contains("value: \"Hello\"", result);
        Assert.Equal(0, invocationCount);
    }
}
