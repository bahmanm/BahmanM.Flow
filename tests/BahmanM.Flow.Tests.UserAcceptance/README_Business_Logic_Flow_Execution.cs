using static BahmanM.Flow.Outcome;

namespace BahmanM.Flow.Tests.UserAcceptance;

public class README_Business_Logic_Flow_Execution
{
    private readonly BillingService _billingService = new();
    private readonly TemplateService _templateService = new();
    private readonly DispatchService _dispatchService = new();

    [Fact]
    [Trait("Category", "Acceptance")]
    public async Task Should_Succeed_When_All_Underlying_Services_Succeed()
    {
        // Arrange
        var coreNoticeFlow = CreateCollectionNoticeFlow(userId: 1, shouldTemplateServiceFail: false);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(coreNoticeFlow);

        // Assert
        var expected = Success(new PostalTrackingId("ABC-123"));
        Assert.Equal(expected, outcome);
    }

    [Fact]
    [Trait("Category", "Acceptance")]
    public async Task Should_Fail_When_An_Intermediate_Service_Fails()
    {
        // Arrange
        var coreNoticeFlow = CreateCollectionNoticeFlow(userId: 1, shouldTemplateServiceFail: true);

        // Act
        var outcome = await FlowEngine.ExecuteAsync(coreNoticeFlow);

        // Assert
        Assert.IsType<Failure<PostalTrackingId>>(outcome);
    }

    private IFlow<PostalTrackingId> CreateCollectionNoticeFlow(int userId, bool shouldTemplateServiceFail)
    {
        var templateService = new TemplateService(shouldTemplateServiceFail);

        return _billingService.GetBillingProfileFlow(userId)
            .Select(profile => new { profile.Fullname, profile.BillingAddress })
            .Chain(data =>
                templateService.GenerateDocumentFlow(
                    "CollectionNotice",
                    data.Fullname,
                    data.BillingAddress
                )
            )
            .Chain(document => _dispatchService.SendByPostFlow(document));
    }
}

#region Mocks and Stubs

internal record BillingProfile(string Fullname, string BillingAddress);
internal record Document(string Content);
internal record PostalTrackingId(string Id);

internal class BillingService
{
    // 📜 Hypatia of Alexandria (c. 350-415 AD) was a Greek Neoplatonist philosopher, astronomer, and mathematician.
    public IFlow<BillingProfile> GetBillingProfileFlow(int userId) =>
        Flow.Succeed(new BillingProfile("Hypatia", "The Lyceum, Athens"));
}

internal class TemplateService(bool _shouldFail = false)
{
    public IFlow<Document> GenerateDocumentFlow(string template, string name, string address)
    {
        return _shouldFail
            ? Flow.Fail<Document>(new InvalidOperationException("Template service failed!"))
            : Flow.Succeed(new Document($"Hello {name} at {address}"));
    }
}

internal class DispatchService
{
    public IFlow<PostalTrackingId> SendByPostFlow(Document doc) =>
        Flow.Succeed(new PostalTrackingId("ABC-123"));
}

#endregion
