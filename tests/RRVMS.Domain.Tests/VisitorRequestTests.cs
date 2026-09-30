using RRVMS.Domain.Common;
using RRVMS.Domain.Entities;
using RRVMS.Domain.Enums;
using RRVMS.Domain.ValueObjects;

namespace RRVMS.Domain.Tests;

public sealed class VisitorRequestTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 10, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void InternalVisitorIsApprovedAfterDetailsAreSubmitted()
    {
        var request = CreateRequest(VisitorType.Internal, 1);
        var visitor = request.Visitors.Single();

        request.SubmitVisitor(visitor.Id, Details("INT-1"), "host", Now);

        Assert.Equal(VisitorRequestStatus.Approved, request.Status);
        Assert.Equal(ScreeningDecision.NotApplicable, visitor.ScreeningDecision);
    }

    [Fact]
    public void ExternalVisitorsSupportPartialApproval()
    {
        var request = CreateReadyForScreeningRequest(2);
        var visitors = request.Visitors.OrderBy(visitor => visitor.Sequence).ToArray();

        request.ReviewVisitors([visitors[0].Id], ScreeningDecision.Approved, VisitorClassification.Visitor, string.Empty, "reviewer", Now);
        Assert.Equal(VisitorRequestStatus.PendingScreening, request.Status);

        request.ReviewVisitors([visitors[1].Id], ScreeningDecision.Rejected, VisitorClassification.Vendor, "Restricted visit.", "reviewer", Now);

        Assert.Equal(VisitorRequestStatus.PartiallyApproved, request.Status);
        Assert.Equal(ReceptionStatus.EntryRejected, visitors[1].ReceptionRecords.Single().Status);
    }

    [Fact]
    public void FinalScreeningDecisionCannotBeOverwritten()
    {
        var request = CreateReadyForScreeningRequest(2);
        var visitor = request.Visitors.First();
        request.ReviewVisitors([visitor.Id], ScreeningDecision.Approved, VisitorClassification.Visitor, string.Empty, "reviewer", Now);

        Assert.Throws<DomainException>(() => request.ReviewVisitors([visitor.Id], ScreeningDecision.Rejected, VisitorClassification.Visitor, "Changed", "reviewer", Now));
    }

    [Fact]
    public void DpsCanOnlyBeReplacedDuringCorrection()
    {
        var request = CreateRequest(VisitorType.External, 1);
        var visitor = request.Visitors.Single();
        request.SubmitVisitor(visitor.Id, Details("EXT-1"), "host", Now);
        request.AttachDps(visitor.Id, "first.pdf", "first.pdf", 100, new string('A', 64), "host", Now);

        Assert.Throws<DomainException>(() => request.AttachDps(visitor.Id, "second.pdf", "second.pdf", 100, new string('B', 64), "host", Now));

        request.SubmitForScreening(Now);
        request.RequestCorrection(visitor.Id, "[\"dpsDocument\"]", "Replace the DPS document.", "{}", "reviewer", Now);
        request.AttachDps(visitor.Id, "second.pdf", "second.pdf", 100, new string('B', 64), "host", Now);

        Assert.Equal(2, visitor.CurrentDpsDocument!.Version);
        Assert.NotNull(visitor.DpsDocuments.Single(document => document.Version == 1).DeletedAt);
    }

    [Fact]
    public void ArrivalOutsideOneHourIsTaggedEarlyOrLate()
    {
        var earlyRequest = CreateApprovedInternalRequest();
        var earlyVisitor = earlyRequest.Visitors.Single();
        var earlyDay = earlyRequest.VisitDays.Single();
        earlyRequest.VerifyEntry(earlyVisitor.Id, earlyDay.Id, true, true, true, string.Empty, Now.AddHours(1), TimeZoneInfo.Utc);
        var early = earlyRequest.CheckIn(earlyVisitor.Id, earlyDay.Id, "B-100", Now, TimeZoneInfo.Utc);

        var lateRequest = CreateApprovedInternalRequest();
        var lateVisitor = lateRequest.Visitors.Single();
        var lateDay = lateRequest.VisitDays.Single();
        lateRequest.VerifyEntry(lateVisitor.Id, lateDay.Id, true, true, true, string.Empty, Now.AddHours(1), TimeZoneInfo.Utc);
        var late = lateRequest.CheckIn(lateVisitor.Id, lateDay.Id, "B-101", Now.AddHours(4), TimeZoneInfo.Utc);

        Assert.Equal(ArrivalStatus.Early, early.ArrivalStatus);
        Assert.Equal(ArrivalStatus.Late, late.ArrivalStatus);
    }

    [Fact]
    public void CheckedInRequestCannotBeRescheduledOrCancelled()
    {
        var request = CreateApprovedInternalRequest();
        var visitor = request.Visitors.Single();
        var day = request.VisitDays.Single();
        request.VerifyEntry(visitor.Id, day.Id, true, true, true, string.Empty, Now.AddHours(1), TimeZoneInfo.Utc);
        request.CheckIn(visitor.Id, day.Id, "B-200", Now.AddHours(2), TimeZoneInfo.Utc);

        Assert.Throws<DomainException>(() => request.Reschedule(VisitWindow.Create(Now.AddDays(2), Now.AddDays(2).AddHours(2), Now), "New date", "host", Now));
        Assert.Throws<DomainException>(() => request.Cancel("Cancelled", Now));
    }

    [Fact]
    public void ClosedRequestReceivesSixYearRetentionDate()
    {
        var request = CreateRequest(VisitorType.External, 1);

        request.Cancel("No longer required.", Now);

        Assert.Equal(Now.AddYears(6), request.RetainUntil);
    }

    [Fact]
    public void RequestLevelCorrectionResetsEveryScreeningDecision()
    {
        var request = CreateReadyForScreeningRequest(2);
        var visitors = request.Visitors.OrderBy(visitor => visitor.Sequence).ToArray();
        request.ReviewVisitors([visitors[0].Id], ScreeningDecision.Approved, VisitorClassification.Visitor, string.Empty, "reviewer", Now);
        request.RequestCorrection(visitors[1].Id, "[\"siteCode\"]", "Correct the site.", "{}", "reviewer", Now);

        request.ReviseRequestDetails("Delhi", VisitPurposeType.Technical, "Technical meeting", "Main office", "host-object-id", "Host User", "Engineering", string.Empty, string.Empty, Now);

        Assert.All(request.Visitors, visitor => Assert.Equal(ScreeningDecision.Pending, visitor.ScreeningDecision));
        Assert.Equal(VisitorRequestStatus.PendingCorrection, request.Status);
    }

    [Fact]
    public void MultiDayVisitCreatesOneReceptionRecordPerVisitorPerDay()
    {
        var request = VisitorRequest.Create(
            RequestNumber.Create(2026, 6, 99),
            "host-object-id",
            "host-object-id",
            "Host User",
            "Engineering",
            string.Empty,
            string.Empty,
            VisitorType.Internal,
            ContractorType.NormalVisitor,
            "Bengaluru",
            VisitPurposeType.Technical,
            "Technical meeting",
            "Main office",
            VisitWindow.Create(Now.AddHours(2), Now.AddDays(2).AddHours(6), Now),
            2,
            Now);

        Assert.Equal(3, request.VisitDays.Count);
        Assert.All(request.Visitors, visitor => Assert.Equal(3, visitor.ReceptionRecords.Count));
    }

    private static VisitorRequest CreateReadyForScreeningRequest(int visitors)
    {
        var request = CreateRequest(VisitorType.External, visitors);
        foreach (var visitor in request.Visitors)
        {
            request.SubmitVisitor(visitor.Id, Details($"EXT-{visitor.Sequence}"), "host", Now);
            request.AttachDps(visitor.Id, $"{visitor.Id}.pdf", $"visitor-{visitor.Sequence}.pdf", 100, new string('A', 64), "host", Now);
        }

        request.SubmitForScreening(Now);
        return request;
    }

    private static VisitorRequest CreateApprovedInternalRequest()
    {
        var request = CreateRequest(VisitorType.Internal, 1);
        request.SubmitVisitor(request.Visitors.Single().Id, Details("INT-2"), "host", Now);
        return request;
    }

    private static VisitorRequest CreateRequest(VisitorType type, int visitors) => VisitorRequest.Create(
        RequestNumber.Create(2026, 6, type == VisitorType.Internal ? 1 : 2),
        "host-object-id",
        "host-object-id",
        "Host User",
        "Engineering",
        string.Empty,
        string.Empty,
        type,
        ContractorType.NormalVisitor,
        "Bengaluru",
        VisitPurposeType.Technical,
        "Technical meeting",
        "Main office",
        VisitWindow.Create(Now.AddHours(2), Now.AddHours(6), Now),
        visitors,
        Now);

    private static VisitorDetails Details(string serial) => new(
        "Alex",
        string.Empty,
        "Morgan",
        "India",
        "Engineer",
        "Example Company",
        "1 Example Road",
        "Bengaluru",
        "India",
        "India",
        "+91",
        "9999999999",
        "alex@example.test",
        "Passport",
        string.Empty,
        [new AssetDetails("Laptop", "Work laptop", serial)]);
}
