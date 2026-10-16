using BahmanM.Flow.Description.Formatting;

namespace BahmanM.Flow.Description.Tests.Unit;

public class DescriptionIdentityTests
{
    [Fact]
    public void Description_Identity_Is_Stable_And_Hash_Derived()
    {
        // Arrange
        var description = (Flow.Succeed("Hello")).AsDescription();

        // Act
        var expectedShortId = Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(
            System.Text.Encoding.ASCII.GetBytes(description.Id)))[..10].ToLowerInvariant();
        var descriptionText = DescriptionFormatting.Describe(description);

        // Assert
        Assert.Equal(32, description.Id.Length);
        Assert.All(description.Id, character => Assert.Contains(character, "0123456789abcdef"));
        Assert.Contains($"[node: {expectedShortId}]", descriptionText);
    }
}
