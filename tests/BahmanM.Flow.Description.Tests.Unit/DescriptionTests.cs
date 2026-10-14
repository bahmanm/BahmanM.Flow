using BahmanM.Flow.Description.Serialisation;
using BahmanM.Flow.Description.Tests.Unit.Support;

namespace BahmanM.Flow.Description.Tests.Unit;

public class DescriptionTests
{
    [Fact]
    public async Task Succeed_Description_Lowers_To_Equivalent_Outcome()
    {
        // Arrange
        var description = DescriptionFlowBuilder.Succeed("Hello");
        var expectedFlow = Flow.Succeed("Hello");

        // Act
        var actual = await FlowEngine.ExecuteAsync(description);
        var expected = await FlowEngine.ExecuteAsync(expectedFlow);

        // Assert
        Assert.Equal(expected, actual);
        Assert.IsType<Ast.Primitive.Succeed<string>>(description.Lower());
    }

    [Fact]
    public async Task Fail_Description_Preserves_Exception_Identity()
    {
        // Arrange
        var exception = new InvalidOperationException("failure");
        var description = DescriptionFlowBuilder.Fail<string>(exception);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(description);

        // Assert
        Assert.Same(exception, Assert.IsType<Failure<string>>(outcome).Exception);
        Assert.IsType<Ast.Primitive.Fail<string>>(description.Lower());
    }

    [Fact]
    public async Task Create_Description_Defers_Operation_Until_Execution()
    {
        // Arrange
        var invocationCount = 0;
        Flow.Operations.Create.Sync<int> operation = () => ++invocationCount;
        var description = DescriptionFlowBuilder.Create(operation);

        // Act
        var invocationCountBeforeExecution = invocationCount;
        var outcome = await FlowEngine.ExecuteAsync(description);

        // Assert
        Assert.Equal(0, invocationCountBeforeExecution);
        Assert.Equal(1, Assert.IsType<Success<int>>(outcome).Value);
        Assert.Equal(1, invocationCount);
        Assert.IsType<Ast.Create.Sync<int>>(description.Lower());
    }

    [Fact]
    public async Task Select_Description_Lowers_And_Defers_Operation()
    {
        // Arrange
        var invocationCount = 0;
        Flow.Operations.Select.Sync<string, int> operation = value =>
        {
            invocationCount++;
            return value.Length;
        };
        var description = DescriptionFlowBuilder.Select(DescriptionFlowBuilder.Succeed("Hello"), operation);
        var expectedFlow = Flow.Succeed("Hello").Select(value => value.Length);

        // Act
        var invocationCountBeforeExecution = invocationCount;
        var actual = await FlowEngine.ExecuteAsync(description);
        var expected = await FlowEngine.ExecuteAsync(expectedFlow);

        // Assert
        Assert.Equal(0, invocationCountBeforeExecution);
        Assert.Equal(expected, actual);
        Assert.Equal(1, invocationCount);
        Assert.IsType<Ast.Select.Sync<string, int>>(description.Lower());
    }

    [Fact]
    public void Description_Identity_Is_Stable_And_Hash_Derived()
    {
        // Arrange
        var description = DescriptionFlowBuilder.Succeed("Hello");

        // Act
        var expectedShortId = Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(
            System.Text.Encoding.ASCII.GetBytes(description.Id)))[..10].ToLowerInvariant();

        // Assert
        Assert.Equal(32, description.Id.Length);
        Assert.All(description.Id, character => Assert.Contains(character, "0123456789abcdef"));
        Assert.Equal(expectedShortId, description.ShortId);
    }

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
