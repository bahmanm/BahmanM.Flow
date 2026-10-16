
namespace BahmanM.Flow.Description.Tests.Unit;

public class PrimitiveDescriptionTests
{
    [Fact]
    public async Task Succeed_Description_Lowers_To_Equivalent_Outcome()
    {
        // Arrange
        var description = (Flow.Succeed("Hello")).AsDescription();
        var expectedFlow = Flow.Succeed("Hello");

        // Act
        var actual = await FlowEngine.ExecuteAsync(description);
        var expected = await FlowEngine.ExecuteAsync(expectedFlow);

        // Assert
        Assert.Equal(expected, actual);
        Assert.IsType<Ast.Primitive.Succeed<string>>(description.Lower(BahmanM.Flow.FacadeBehaviourStrategyFactory.Instance));
    }

    [Fact]
    public async Task Fail_Description_Preserves_Exception_Identity()
    {
        // Arrange
        var exception = new InvalidOperationException("failure");
        var description = (Flow.Fail<string>(exception)).AsDescription();

        // Act
        var outcome = await FlowEngine.ExecuteAsync(description);

        // Assert
        Assert.Same(exception, Assert.IsType<Failure<string>>(outcome).Exception);
        Assert.IsType<Ast.Primitive.Fail<string>>(description.Lower(BahmanM.Flow.FacadeBehaviourStrategyFactory.Instance));
    }
}
