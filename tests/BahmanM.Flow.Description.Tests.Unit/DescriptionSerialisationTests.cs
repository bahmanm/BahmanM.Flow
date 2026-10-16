using BahmanM.Flow.Description.Serialisation;

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
        var description = (((Flow.Succeed("Hello")).AsDescription()).Select(operation)).AsDescription();

        // Act
        var result = DescriptionFormatter.Describe(description);

        // Assert
        Assert.Contains("Select.Sync<string, int>", result);
        Assert.Contains("operation: lambda(string) -> int", result);
        Assert.Contains("Succeed<string>", result);
        Assert.Contains("value: \"Hello\"", result);
        Assert.Equal(0, invocationCount);
    }

    [Fact]
    public void Describe_All_Shows_Ordered_Flow_Children_Without_Executing_Them()
    {
        // Arrange
        var invocationCount = 0;
        Flow.Operations.Create.Sync<int> first = () =>
        {
            invocationCount++;
            return 1;
        };
        Flow.Operations.Create.Sync<int> second = () =>
        {
            invocationCount++;
            return 2;
        };
        var description = (Flow.All((Flow.Create(() => first())).AsDescription(), (Flow.Create(() => second())).AsDescription())).AsDescription();

        // Act
        var result = DescriptionFormatter.Describe(description);

        // Assert
        Assert.Contains("All<int>", result);
        Assert.Contains("flow[0]: Create.Sync<int>", result);
        Assert.Contains("flow[1]: Create.Sync<int>", result);
        Assert.True(result.IndexOf("flow[0]", StringComparison.Ordinal) < result.IndexOf("flow[1]", StringComparison.Ordinal));
        Assert.Equal(0, invocationCount);
    }

    [Fact]
    public void Describe_Any_Shows_Ordered_Flow_Children_Without_Executing_Them()
    {
        // Arrange
        var invocationCount = 0;
        Flow.Operations.Create.Sync<int> first = () =>
        {
            invocationCount++;
            return 1;
        };
        Flow.Operations.Create.Sync<int> second = () =>
        {
            invocationCount++;
            return 2;
        };
        var description = (Flow.Any((Flow.Create(() => first())).AsDescription(), (Flow.Create(() => second())).AsDescription())).AsDescription();

        // Act
        var result = DescriptionFormatter.Describe(description);

        // Assert
        Assert.Contains("Any<int>", result);
        Assert.Contains("flow[0]: Create.Sync<int>", result);
        Assert.Contains("flow[1]: Create.Sync<int>", result);
        Assert.True(result.IndexOf("flow[0]", StringComparison.Ordinal) < result.IndexOf("flow[1]", StringComparison.Ordinal));
        Assert.Equal(0, invocationCount);
    }
}
