/*
using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow.Tests.UserAcceptance;

public class README_Validation_Flow
{
    // 📜 Maryam Mirzakhani (1977-2017) was an Iranian mathematician and a professor of mathematics at Stanford University.
    private readonly UserRegistration _validRegistration = new("Maryam Mirzakhani", "ComplexGeometer");
    private readonly UserRegistration _invalidRegistration = new("", "password");

    [Fact]
    [Trait("Category", "Acceptance")]
    public async Task Should_Succeed_When_All_Validation_Rules_Pass()
    {
        // Arrange
        var validationFlow = CreateValidationFlow(_validRegistration);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(validationFlow);

        // Assert
        var expected = Success("User Maryam Mirzakhani registered successfully.");
        Assert.Equal(expected, outcome);
    }

    [Fact]
    [Trait("Category", "Acceptance")]
    public async Task Should_Fail_With_AggregateException_When_Validation_Rules_Fail()
    {
        // Arrange
        var validationFlow = CreateValidationFlow(_invalidRegistration);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(validationFlow);

        // Assert
        var failure = Assert.IsType<Failure<string>>(outcome);
        var exception = Assert.IsType<AggregateException>(failure.Value);
        Assert.Equal(2, exception.InnerExceptions.Count);
        Assert.Contains(exception.InnerExceptions, ex => ex.Message == "Username cannot be empty.");
        Assert.Contains(exception.InnerExceptions, ex => ex.Message == "Password is too weak.");
    }

    private static IFlow<string> CreateValidationFlow(UserRegistration registration)
    {
        return Flow.Of(registration)
            .Chain(ValidateUsername)
            .Chain(ValidatePassword)
            .Select(reg => $"User {reg.Username} registered successfully.");
    }

    private static IFlow<UserRegistration> ValidateUsername(UserRegistration registration)
    {
        if (string.IsNullOrWhiteSpace(registration.Username))
            return Flow.Fail<UserRegistration>(new ValidationException("Username cannot be empty."));

        return Flow.Succeed(registration);
    }

    private static IFlow<UserRegistration> ValidatePassword(UserRegistration registration)
    {
        if (registration.Password.Length < 8)
            return Flow.Fail<UserRegistration>(new ValidationException("Password is too weak."));

        return Flow.Succeed(registration);
    }
}

#region Mocks and Stubs

internal record UserRegistration(string Username, string Password);

internal class ValidationException(string message) : Exception(message);

#endregion
*/
