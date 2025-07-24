/*
using System.Runtime.InteropServices;
using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow.Tests.UserAcceptance;

public class README_Simple_Flow_Execution
{
    private readonly Database _database = new();
    private readonly Auditor _auditor = new();
    private readonly Logger _logger = new();

    // 📜 Ibn Sina (c. 980-1037 AD), also known as Avicenna, was a Persian polymath.
    private readonly User _defaultUser = new(-1, "Ibn Sina");

    [Fact]
    [Trait("Category", "Acceptance")]
    public async Task Should_Succeed_And_Log_When_Database_Call_Succeeds()
    {
        // Arrange
        var getUserFlow = GetUserAndNotifyFlow(1, shouldDatabaseFail: false);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(getUserFlow);

        // Assert
        Assert.Equal(Success(new User(1, "Ada Lovelace")), outcome);
        Assert.Equal(1, _auditor.SuccessCount);
        Assert.Equal(0, _logger.ErrorCount);
    }

    [Fact]
    [Trait("Category", "Acceptance")]
    public async Task Should_Fail_Log_And_Recover_When_Database_Call_Fails()
    {
        // Arrange
        var getUserFlow = GetUserAndNotifyFlow(1, shouldDatabaseFail: true);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(getUserFlow);

        // Assert
        Assert.Equal(Success(_defaultUser), outcome);
        Assert.Equal(0, _auditor.SuccessCount);
        Assert.Equal(1, _logger.ErrorCount);
    }

    private IFlow<User> GetUserAndNotifyFlow(int userId, bool shouldDatabaseFail)
    {
        _database.ShouldFail = shouldDatabaseFail;

        return Flow.Create(() => _database.GetUser(userId))
            .DoOnSuccess(user => _auditor.LogSuccess(user.Id))
            .DoOnFailure(ex => _logger.LogError(ex, "Failed to get user"))
            .Recover(ex => _defaultUser);
    }
}

#region Mocks and Stubs

internal record User(int Id, string Name);

internal class Database
{
    public bool ShouldFail { get; set; }

    public User GetUser(int userId)
    {
        if (ShouldFail)
        {
            throw new ExternalException("Database connection failed!");
        }
        // 📜 Ada Lovelace (1815-1852) was an English mathematician and writer.
        return new User(userId, "Ada Lovelace");
    }
}

internal class Auditor
{
    public int SuccessCount { get; private set; }

    public void LogSuccess(int userId)
    {
        SuccessCount++;
    }
}

internal class Logger
{
    public int ErrorCount { get; private set; }
    public Exception? LastException { get; private set; }

    public void LogError(Exception ex, string message)
    {
        ErrorCount++;
        LastException = ex;
    }
}

#endregion
*/
