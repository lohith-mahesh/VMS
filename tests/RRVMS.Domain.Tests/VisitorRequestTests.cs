using RRVMS.Domain.Common;
using RRVMS.Domain.Entities;
using RRVMS.Domain.Enums;
using RRVMS.Domain.ValueObjects;

namespace RRVMS.Domain.Tests;

public sealed class VisitorRequestTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 10, 8, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(VisitorType.Internal)]
    [InlineData(VisitorType.External)]
    public void BothVisitorTypesRequireHostAndExportControlReview(VisitorType type)
    {
        var request = CreateRequest(type, 1);
        var visitor = request.Visitors.Single();
        request.SubmitVisitor(visitor.Id, Details(1), "host", Now);
        Assert.Equal(VisitorRequestStatus.VisitorFormSubmitted, request.Status);
        Assert.Equal(ScreeningDecision.Pending, visitor.ScreeningDecision);
        Assert.Throws<DomainException>(() => request.SubmitForScreening(Now));
        request.StartHostReview(Now);
        request.SubmitForScreening(Now);
        request.ReviewVisitors([visitor.Id], ScreeningDecision.Approved, VisitorClassification.Visitor, string.Empty, "reviewer", Now);
        Assert.Equal(VisitorRequestStatus.Approved, request.Status);
    }

    [Fact]
    public void DpsDocumentIsOptionalAndReplaceableBeforeSubmission()
    {
        var request = CreateRequest(VisitorType.External, 1);
        var visitor = request.Visitors.Single();
        request.AttachDps(visitor.Id, "first.pdf", "first.pdf", 100, new string('A', 64), "host", Now);
        request.AttachDps(visitor.Id, "second.pdf", "second.pdf", 100, new string('B', 64), "host", Now);
        Assert.Equal(2, visitor.CurrentDpsDocument!.Version);
        Assert.NotNull(visitor.DpsDocuments.Single(document => document.Version == 1).DeletedAt);
        request.SubmitVisitor(visitor.Id, Details(1), "host", Now);
        request.StartHostReview(Now);
        request.SubmitForScreening(Now);
    }

    [Fact]
    public void PartialApprovalAndFinalDecisionAreTrackedPerVisitor()
    {
        var request = CreateReviewableRequest(2);
        var visitors = request.Visitors.OrderBy(visitor => visitor.Sequence).ToArray();
        request.ReviewVisitors([visitors[0].Id], ScreeningDecision.Approved, VisitorClassification.Visitor, string.Empty, "reviewer", Now);
        Assert.Equal(VisitorRequestStatus.PendingEcReview, request.Status);
        request.ReviewVisitors([visitors[1].Id], ScreeningDecision.Rejected, VisitorClassification.Vendor, "Restricted visit.", "reviewer", Now);
        Assert.Equal(VisitorRequestStatus.PartiallyApproved, request.Status);
        Assert.Equal(ReceptionStatus.EntryRejected, visitors[1].ReceptionRecords.Single().Status);
    }

    [Fact]
    public void RequestedFieldMustChangeBeforeCorrectionIsSubmitted()
    {
        var request = CreateReviewableRequest(1);
        var visitor = request.Visitors.Single();
        request.RequestCorrection(visitor.Id, "[\"designation\"]", "Update designation.", "{}", "reviewer", Now);
        Assert.Equal(VisitorRequestStatus.PendingDocumentation, request.Status);
        Assert.Throws<DomainException>(() => request.SubmitVisitor(visitor.Id, Details(1), "host", Now));
        request.SubmitVisitor(visitor.Id, Details(1) with { Designation = "Manager" }, "host", Now);
        request.ResolveCorrections(visitor.Id, Now);
        Assert.Equal(VisitorRequestStatus.DocumentationSubmitted, request.Status);
    }

    [Fact]
    public void DuplicateScreeningRemarkIsRejectedForTheSameVisitor()
    {
        var request = CreateReviewableRequest(1);
        var visitorId = request.Visitors.Single().Id;
        request.AddScreeningRemark(visitorId, "Review identity", "reviewer", "ExportControl", "correlation", Now);
        Assert.Throws<DomainException>(() => request.AddScreeningRemark(visitorId, "review IDENTITY", "reviewer", "ExportControl", "correlation", Now));
    }

    [Fact]
    public void ReceptionCompletesAfterEntryRejection()
    {
        var request = CreateApprovedRequest();
        var visitor = request.Visitors.Single();
        var day = request.VisitDays.Single();
        request.VerifyEntry(visitor.Id, day.Id, false, false, false, "Identity mismatch", Now.AddHours(1), TimeZoneInfo.Utc);
        Assert.Equal(VisitorRequestStatus.VisitProcessCompleted, request.Status);
    }

    [Fact]
    public void ReceptionVerificationPreventsReschedule()
    {
        var request = CreateApprovedRequest();
        var visitor = request.Visitors.Single();
        var day = request.VisitDays.Single();
        request.VerifyEntry(visitor.Id, day.Id, true, true, true, string.Empty, Now.AddHours(1), TimeZoneInfo.Utc);
        Assert.Throws<DomainException>(() => request.Reschedule(VisitWindow.Create(Now.AddDays(2), Now.AddDays(2).AddHours(2), Now), string.Empty, "host", Now));
    }

    [Fact]
    public void MultiDayVisitCreatesOneReceptionRecordPerVisitorPerDay()
    {
        var request = VisitorRequest.Create(RequestNumber.Create(2026, 99), "host", "host", "Host User", "Engineering", string.Empty, string.Empty,
            VisitorType.Internal, ContractorType.NormalVisitor, "Bengaluru", VisitPurposeType.Technical, "Technical meeting", "Main office",
            VisitWindow.Create(Now.AddHours(2), Now.AddDays(2).AddHours(6), Now), 2, Now);
        Assert.Equal(3, request.VisitDays.Count);
        Assert.All(request.Visitors, visitor => Assert.Equal(3, visitor.ReceptionRecords.Count));
    }

    private static VisitorRequest CreateApprovedRequest()
    {
        var request = CreateReviewableRequest(1);
        request.ReviewVisitors([request.Visitors.Single().Id], ScreeningDecision.Approved, VisitorClassification.Visitor, string.Empty, "reviewer", Now);
        return request;
    }

    private static VisitorRequest CreateReviewableRequest(int visitors)
    {
        var request = CreateRequest(VisitorType.External, visitors);
        foreach (var visitor in request.Visitors) request.SubmitVisitor(visitor.Id, Details(visitor.Sequence), "host", Now);
        request.StartHostReview(Now);
        request.SubmitForScreening(Now);
        return request;
    }

    private static VisitorRequest CreateRequest(VisitorType type, int visitors) => VisitorRequest.Create(
        RequestNumber.Create(2026, type == VisitorType.Internal ? 1 : 2), "host", "host", "Host User", "Engineering", string.Empty, string.Empty,
        type, ContractorType.NormalVisitor, "Bengaluru", VisitPurposeType.Technical, "Technical meeting", "Main office",
        VisitWindow.Create(Now.AddHours(2), Now.AddHours(6), Now), visitors, Now);

    private static VisitorDetails Details(int sequence) => new(
        "Alex", string.Empty, $"Morgan{sequence}", "India", "Engineer", "Example Company", "1 Example Road", "Bengaluru", "India", "India", "91",
        $"999999999{sequence}", "alex@example.test", "Passport", string.Empty, [new AssetDetails("Laptop", "Work laptop", $"INT-{sequence}")]);
}
