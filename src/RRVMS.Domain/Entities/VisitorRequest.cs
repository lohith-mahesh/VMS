using System.Text.Json;
using System.Text.Json.Serialization;
using RRVMS.Domain.Common;
using RRVMS.Domain.Enums;
using RRVMS.Domain.ValueObjects;

namespace RRVMS.Domain.Entities;

public sealed class VisitorRequest : Entity
{
    private static readonly JsonSerializerOptions AuditJsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly List<AuditEvent> _auditEvents = [];
    private readonly List<InformationRequest> _informationRequests = [];
    private readonly List<ScheduleChange> _scheduleChanges = [];
    private readonly List<ScreeningReview> _screeningReviews = [];
    private readonly List<VisitDay> _visitDays = [];
    private readonly List<Visitor> _visitors = [];

    private VisitorRequest()
    {
    }

    private VisitorRequest(
        RequestNumber requestNumber,
        string requesterObjectId,
        string mainHostObjectId,
        string mainHostName,
        string hostDepartment,
        string escortingHostObjectId,
        string escortingHostName,
        VisitorType visitorType,
        ContractorType contractorType,
        string siteCode,
        VisitPurposeType purposeType,
        string purpose,
        string areasToVisit,
        VisitWindow visitWindow,
        int visitorCount,
        DateTimeOffset now)
    {
        RequestNumber = requestNumber.Value;
        RequesterObjectId = requesterObjectId;
        MainHostObjectId = mainHostObjectId;
        MainHostName = mainHostName.Trim();
        HostDepartment = hostDepartment.Trim();
        EscortingHostObjectId = escortingHostObjectId;
        EscortingHostName = escortingHostName.Trim();
        VisitorType = visitorType;
        ContractorType = visitorType == VisitorType.Internal ? ContractorType.NormalVisitor : contractorType;
        SiteCode = siteCode.Trim();
        PurposeType = purposeType;
        Purpose = purpose.Trim();
        AreasToVisit = areasToVisit.Trim();
        VisitStart = visitWindow.Start;
        VisitEnd = visitWindow.End;
        Status = VisitorRequestStatus.Draft;
        CreatedAt = now;
        UpdatedAt = now;
        ReplaceSchedule(visitWindow);

        for (var sequence = 1; sequence <= visitorCount; sequence++)
        {
            _visitors.Add(new Visitor(Id, sequence, _visitDays, visitorType == VisitorType.Internal));
        }
    }

    public string RequestNumber { get; private set; } = string.Empty;
    public string RequesterObjectId { get; private set; } = string.Empty;
    public string MainHostObjectId { get; private set; } = string.Empty;
    public string MainHostName { get; private set; } = string.Empty;
    public string HostDepartment { get; private set; } = string.Empty;
    public string EscortingHostObjectId { get; private set; } = string.Empty;
    public string EscortingHostName { get; private set; } = string.Empty;
    public VisitorType VisitorType { get; private set; }
    public ContractorType ContractorType { get; private set; }
    public string SiteCode { get; private set; } = string.Empty;
    public VisitPurposeType PurposeType { get; private set; }
    public string Purpose { get; private set; } = string.Empty;
    public string AreasToVisit { get; private set; } = string.Empty;
    public VisitorRequestStatus Status { get; private set; }
    public DateTimeOffset VisitStart { get; private set; }
    public DateTimeOffset VisitEnd { get; private set; }
    public string CancellationReason { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public DateTimeOffset? RejectedAt { get; private set; }
    public DateTimeOffset? RetainUntil { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyCollection<Visitor> Visitors => _visitors.AsReadOnly();
    public IReadOnlyCollection<VisitDay> VisitDays => _visitDays.AsReadOnly();
    public IReadOnlyCollection<AuditEvent> AuditEvents => _auditEvents.AsReadOnly();
    public IReadOnlyCollection<InformationRequest> InformationRequests => _informationRequests.AsReadOnly();
    public IReadOnlyCollection<ScheduleChange> ScheduleChanges => _scheduleChanges.AsReadOnly();
    public IReadOnlyCollection<ScreeningReview> ScreeningReviews => _screeningReviews.AsReadOnly();

    public static VisitorRequest Create(
        RequestNumber requestNumber,
        string requesterObjectId,
        string mainHostObjectId,
        string mainHostName,
        string hostDepartment,
        string escortingHostObjectId,
        string escortingHostName,
        VisitorType visitorType,
        ContractorType contractorType,
        string siteCode,
        VisitPurposeType purposeType,
        string purpose,
        string areasToVisit,
        VisitWindow visitWindow,
        int visitorCount,
        DateTimeOffset now)
    {
        if (visitorCount is < 1 or > 20)
        {
            throw new DomainException("The number of visitors must be between 1 and 20.");
        }

        if (string.IsNullOrWhiteSpace(requesterObjectId) || string.IsNullOrWhiteSpace(mainHostName) || string.IsNullOrWhiteSpace(siteCode) || string.IsNullOrWhiteSpace(purpose))
        {
            throw new DomainException("The requester, host, site, and purpose are required.");
        }

        return new VisitorRequest(
            requestNumber, requesterObjectId, mainHostObjectId, mainHostName, hostDepartment,
            escortingHostObjectId, escortingHostName, visitorType, contractorType, siteCode,
            purposeType, purpose, areasToVisit, visitWindow, visitorCount, now);
    }

    public Visitor Visitor(Guid visitorId) =>
        _visitors.SingleOrDefault(visitor => visitor.Id == visitorId)
        ?? throw new DomainException("The visitor does not belong to this request.");

    public VisitDay VisitDay(Guid visitDayId) =>
        _visitDays.SingleOrDefault(day => day.Id == visitDayId)
        ?? throw new DomainException("The visit day does not belong to this request.");

    public ReceptionRecord ReceptionRecord(Guid visitorId, Guid visitDayId) =>
        Visitor(visitorId).ReceptionRecord(visitDayId);

    public void SubmitVisitor(Guid visitorId, VisitorDetails details, string actorId, DateTimeOffset now)
    {
        EnsureHostEditable();
        var visitor = Visitor(visitorId);
        visitor.SubmitDetails(details, actorId, now);
        var allSubmitted = _visitors.All(item => item.DetailsStatus == VisitorDetailsStatus.Submitted);

        if (!allSubmitted)
        {
            Status = VisitorRequestStatus.VisitorDetailsPending;
        }
        else if (VisitorType == VisitorType.Internal)
        {
            foreach (var item in _visitors)
            {
                item.AutoApprove(now);
            }

            Status = VisitorRequestStatus.Approved;
            SubmittedAt = now;
            ApprovedAt = now;
        }
        else
        {
            Status = VisitorRequestStatus.ReadyForScreening;
        }

        UpdatedAt = now;
    }

    public void SubmitForScreening(DateTimeOffset now)
    {
        if (VisitorType != VisitorType.External || Status != VisitorRequestStatus.ReadyForScreening)
        {
            throw new DomainException("The request is not ready for Export Control screening.");
        }

        if (_visitors.Any(visitor => visitor.DetailsStatus != VisitorDetailsStatus.Submitted || visitor.CurrentDpsDocument is null))
        {
            throw new DomainException("Every external visitor requires completed details and a DPS PDF.");
        }

        Status = VisitorRequestStatus.PendingScreening;
        SubmittedAt = now;
        UpdatedAt = now;
    }

    public DpsDocument AttachDps(Guid visitorId, string blobName, string fileName, long size, string sha256, string actorId, DateTimeOffset now)
    {
        EnsureHostEditable();
        if (VisitorType != VisitorType.External)
        {
            throw new DomainException("DPS documents only apply to external visitors.");
        }

        var visitor = Visitor(visitorId);
        var document = visitor.AttachDps(
            blobName,
            fileName,
            size,
            sha256,
            actorId,
            now,
            visitor.DetailsStatus == VisitorDetailsStatus.RevisionRequired);
        UpdatedAt = now;
        return document;
    }

    public void ReviseRequestDetails(
        string siteCode,
        VisitPurposeType purposeType,
        string purpose,
        string areasToVisit,
        string mainHostObjectId,
        string mainHostName,
        string hostDepartment,
        string escortingHostObjectId,
        string escortingHostName,
        DateTimeOffset now)
    {
        if (Status != VisitorRequestStatus.PendingCorrection)
        {
            throw new DomainException("Request-level details can only be changed while corrections are requested.");
        }

        if (string.IsNullOrWhiteSpace(siteCode) || string.IsNullOrWhiteSpace(purpose) || string.IsNullOrWhiteSpace(mainHostObjectId) || string.IsNullOrWhiteSpace(mainHostName))
        {
            throw new DomainException("The site, purpose, and main host are required.");
        }

        SiteCode = siteCode.Trim();
        PurposeType = purposeType;
        Purpose = purpose.Trim();
        AreasToVisit = areasToVisit.Trim();
        MainHostObjectId = mainHostObjectId.Trim();
        MainHostName = mainHostName.Trim();
        HostDepartment = hostDepartment.Trim();
        EscortingHostObjectId = escortingHostObjectId.Trim();
        EscortingHostName = escortingHostName.Trim();
        foreach (var visitor in _visitors)
        {
            visitor.ResetForRequestRevision();
        }

        ApprovedAt = null;
        RejectedAt = null;
        UpdatedAt = now;
    }

    public void RequestCorrection(Guid visitorId, string fieldsJson, string instructions, string originalValuesJson, string actorId, DateTimeOffset now)
    {
        EnsureScreeningState();
        if (string.IsNullOrWhiteSpace(instructions) || fieldsJson == "[]")
        {
            throw new DomainException("Correction fields and instructions are required.");
        }

        var visitor = Visitor(visitorId);
        if (visitor.ScreeningDecision != ScreeningDecision.Pending)
        {
            throw new DomainException("Corrections can only be requested before a visitor has a final screening decision.");
        }

        if (_informationRequests.Any(item => item.VisitorId == visitorId && item.Status == InformationRequestStatus.Pending))
        {
            throw new DomainException("This visitor already has an open correction request.");
        }

        visitor.RequestRevision();
        _informationRequests.Add(new InformationRequest(Id, visitorId, fieldsJson, instructions, originalValuesJson, actorId, now));
        Status = VisitorRequestStatus.PendingCorrection;
        UpdatedAt = now;
    }

    public void ResolveCorrections(Guid visitorId, string changesJson, DateTimeOffset now)
    {
        foreach (var informationRequest in _informationRequests.Where(item => item.VisitorId == visitorId && item.Status == InformationRequestStatus.Pending))
        {
            informationRequest.Resolve(changesJson, now);
        }

        Status = _informationRequests.Any(item => item.Status == InformationRequestStatus.Pending)
            ? VisitorRequestStatus.PendingCorrection
            : VisitorRequestStatus.CorrectionSubmitted;
        UpdatedAt = now;
    }

    public void ReviewVisitors(IEnumerable<Guid> visitorIds, ScreeningDecision decision, VisitorClassification classification, string comments, string actorId, DateTimeOffset now)
    {
        EnsureScreeningState();
        var selected = visitorIds.Distinct().Select(Visitor).ToArray();
        if (selected.Length == 0)
        {
            throw new DomainException("At least one visitor must be selected.");
        }

        if (selected.Any(visitor => visitor.ScreeningDecision != ScreeningDecision.Pending))
        {
            throw new DomainException("Only visitors awaiting a decision can be reviewed.");
        }

        foreach (var visitor in selected)
        {
            if (decision == ScreeningDecision.Approved)
            {
                visitor.Approve(classification, comments, now);
            }
            else if (decision == ScreeningDecision.Rejected)
            {
                visitor.Reject(classification, comments, now);
            }
            else
            {
                throw new DomainException("The screening decision is invalid.");
            }

            _screeningReviews.Add(new ScreeningReview(Id, visitor.Id, decision, classification, comments.Trim(), actorId, now));
        }

        RefreshScreeningStatus(now);
        UpdatedAt = now;
    }

    public void Reschedule(VisitWindow window, string reason, string actorId, DateTimeOffset now)
    {
        if (Status is VisitorRequestStatus.Cancelled or VisitorRequestStatus.VisitCompleted)
        {
            throw new DomainException("A closed request cannot be rescheduled.");
        }

        if (_visitors.SelectMany(visitor => visitor.ReceptionRecords).Any(record => record.Status is ReceptionStatus.CheckedIn or ReceptionStatus.Completed))
        {
            throw new DomainException("The request cannot be rescheduled after a visitor checks in.");
        }

        if (window.Start == VisitStart && window.End == VisitEnd)
        {
            throw new DomainException("The new visit schedule must differ from the current schedule.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("A rescheduling reason is required.");
        }

        _scheduleChanges.Add(new ScheduleChange(Id, VisitStart, VisitEnd, window.Start, window.End, reason.Trim(), actorId, now));
        ReplaceSchedule(window);
        foreach (var visitor in _visitors)
        {
            visitor.ReplaceReceptionSchedule(_visitDays);
            if (VisitorType == VisitorType.External)
            {
                visitor.ResetScreening();
            }
        }

        if (VisitorType == VisitorType.Internal)
        {
            Status = VisitorRequestStatus.Approved;
            ApprovedAt = now;
        }
        else
        {
            Status = _visitors.All(visitor => visitor.DetailsStatus == VisitorDetailsStatus.Submitted)
                ? VisitorRequestStatus.ReadyForScreening
                : VisitorRequestStatus.VisitorDetailsPending;
            ApprovedAt = null;
            RejectedAt = null;
        }

        UpdatedAt = now;
    }

    public void Cancel(string reason, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("A cancellation reason is required.");
        }

        if (Status is VisitorRequestStatus.Cancelled or VisitorRequestStatus.VisitCompleted or VisitorRequestStatus.Rejected)
        {
            throw new DomainException("A closed request cannot be cancelled.");
        }

        if (_visitors.SelectMany(visitor => visitor.ReceptionRecords).Any(record => record.Status is ReceptionStatus.CheckedIn or ReceptionStatus.Completed))
        {
            throw new DomainException("A request cannot be cancelled after a visitor checks in.");
        }

        CancellationReason = reason.Trim();
        Status = VisitorRequestStatus.Cancelled;
        RetainUntil = now.AddYears(6);
        foreach (var record in _visitors.SelectMany(visitor => visitor.ReceptionRecords))
        {
            record.Cancel();
        }

        UpdatedAt = now;
    }

    public ReceptionRecord VerifyEntry(Guid visitorId, Guid visitDayId, bool approved, bool identityConfirmed, bool assetsConfirmed, string remarks, DateTimeOffset now, TimeZoneInfo timeZone)
    {
        EnsureReceptionEligible(visitorId, visitDayId);
        EnsureCurrentVisitDay(visitDayId, now, timeZone);
        var visitor = Visitor(visitorId);
        var record = visitor.ReceptionRecord(visitDayId);

        if (approved)
        {
            if (!identityConfirmed || (visitor.Assets.Count > 0 && !assetsConfirmed))
            {
                throw new DomainException("Identity and declared assets must be confirmed before approval.");
            }

            record.ApproveVerification(visitor.Assets.Count > 0, remarks);
            foreach (var asset in visitor.Assets)
            {
                asset.MarkVerified();
            }
        }
        else
        {
            record.RejectEntry(remarks);
            foreach (var asset in visitor.Assets)
            {
                asset.MarkRejected();
            }
        }

        return record;
    }

    public ReceptionRecord CheckIn(Guid visitorId, Guid visitDayId, string badgeId, DateTimeOffset now, TimeZoneInfo timeZone)
    {
        EnsureReceptionEligible(visitorId, visitDayId);
        var visitor = Visitor(visitorId);
        if (visitor.ReceptionRecords.Any(record => record.Status == ReceptionStatus.CheckedIn))
        {
            throw new DomainException("The visitor is already checked in.");
        }

        var day = VisitDay(visitDayId);
        EnsureCurrentVisitDay(visitDayId, now, timeZone);
        var expectedLocal = day.Date.ToDateTime(day.ExpectedArrival, DateTimeKind.Unspecified);
        var expected = new DateTimeOffset(expectedLocal, timeZone.GetUtcOffset(expectedLocal));
        var record = visitor.ReceptionRecord(visitDayId);
        record.CheckIn(badgeId, BadgeType(visitor), now, expected);
        UpdatedAt = now;
        return record;
    }

    public ReceptionRecord CheckOut(Guid visitorId, Guid visitDayId, DateTimeOffset now)
    {
        var record = Visitor(visitorId).ReceptionRecord(visitDayId);
        record.CheckOut(now);
        RefreshCompletion(now);
        UpdatedAt = now;
        return record;
    }

    public ReceptionRecord MarkNoShow(Guid visitorId, Guid visitDayId, DateOnly today, DateTimeOffset now)
    {
        var day = VisitDay(visitDayId);
        if (day.Date > today)
        {
            throw new DomainException("A future visit cannot be marked as a no-show.");
        }

        var record = Visitor(visitorId).ReceptionRecord(visitDayId);
        record.MarkNoShow();
        RefreshCompletion(now);
        UpdatedAt = now;
        return record;
    }

    public AuditEvent RecordAudit(string action, string actorId, string actorRole, object? before, object? after, string details, string correlationId, DateTimeOffset now)
    {
        var audit = new AuditEvent(
            Id,
            action,
            actorId,
            actorRole,
            before is null ? string.Empty : JsonSerializer.Serialize(before, AuditJsonOptions),
            after is null ? string.Empty : JsonSerializer.Serialize(after, AuditJsonOptions),
            details,
            correlationId,
            now);
        _auditEvents.Add(audit);
        UpdatedAt = now;
        return audit;
    }

    public string BadgeType(Visitor visitor)
    {
        if (ContractorType == RRVMS.Domain.Enums.ContractorType.FacilitiesContractor || visitor.Classification == VisitorClassification.Vendor)
        {
            return "Orange - Vendor";
        }

        return ContractorType == RRVMS.Domain.Enums.ContractorType.GtreContractor ? "Red - GTRE" : "Red - Visitor";
    }

    private void EnsureHostEditable()
    {
        var editable = Status is VisitorRequestStatus.Draft
            or VisitorRequestStatus.VisitorDetailsPending
            or VisitorRequestStatus.ReadyForScreening
            or VisitorRequestStatus.PendingCorrection;
        if (!editable)
        {
            throw new DomainException("Visitor details are locked at the current workflow stage.");
        }
    }

    private void EnsureScreeningState()
    {
        if (Status is not (VisitorRequestStatus.PendingScreening or VisitorRequestStatus.CorrectionSubmitted or VisitorRequestStatus.PendingCorrection))
        {
            throw new DomainException("The request is not available for Export Control review.");
        }
    }

    private void EnsureReceptionEligible(Guid visitorId, Guid visitDayId)
    {
        if (Status is not (VisitorRequestStatus.Approved or VisitorRequestStatus.PartiallyApproved))
        {
            throw new DomainException("The request is not approved for reception processing.");
        }

        var visitor = Visitor(visitorId);
        if (visitor.ScreeningDecision is not (ScreeningDecision.Approved or ScreeningDecision.NotApplicable))
        {
            throw new DomainException("The visitor is not approved for entry.");
        }

        VisitDay(visitDayId);
    }

    private void EnsureCurrentVisitDay(Guid visitDayId, DateTimeOffset now, TimeZoneInfo timeZone)
    {
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, timeZone).Date);
        if (VisitDay(visitDayId).Date != today)
        {
            throw new DomainException("Reception processing is only available on the scheduled visit day.");
        }
    }

    private void ReplaceSchedule(VisitWindow window)
    {
        _visitDays.Clear();
        _visitDays.AddRange(window.Expand().Select(day => new VisitDay(Id, day.Date, day.Arrival, day.Departure)));
        VisitStart = window.Start;
        VisitEnd = window.End;
    }

    private void RefreshScreeningStatus(DateTimeOffset now)
    {
        var decisions = _visitors.Select(visitor => visitor.ScreeningDecision).ToArray();
        if (decisions.Any(decision => decision == ScreeningDecision.Pending))
        {
            Status = VisitorRequestStatus.PendingScreening;
            return;
        }

        if (decisions.All(decision => decision == ScreeningDecision.Approved))
        {
            Status = VisitorRequestStatus.Approved;
            ApprovedAt = now;
            RejectedAt = null;
            return;
        }

        if (decisions.All(decision => decision == ScreeningDecision.Rejected))
        {
            Status = VisitorRequestStatus.Rejected;
            RejectedAt = now;
            ApprovedAt = null;
            RetainUntil = now.AddYears(6);
            return;
        }

        Status = VisitorRequestStatus.PartiallyApproved;
        ApprovedAt = now;
        RejectedAt = null;
    }

    private void RefreshCompletion(DateTimeOffset now)
    {
        var finalStatuses = new[]
        {
            ReceptionStatus.Completed,
            ReceptionStatus.NoShow,
            ReceptionStatus.EntryRejected,
            ReceptionStatus.Cancelled
        };
        if (_visitors.SelectMany(visitor => visitor.ReceptionRecords).All(record => finalStatuses.Contains(record.Status)))
        {
            Status = VisitorRequestStatus.VisitCompleted;
            RetainUntil = now.AddYears(6);
        }
    }
}
