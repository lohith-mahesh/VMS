using System.Text.Json;
using RRVMS.Domain.Common;
using RRVMS.Domain.Enums;
using RRVMS.Domain.ValueObjects;

namespace RRVMS.Domain.Entities;

public sealed class Visitor : Entity
{
    private readonly List<DeclaredAsset> _assets = [];
    private readonly List<DpsDocument> _dpsDocuments = [];
    private readonly List<ReceptionRecord> _receptionRecords = [];
    private readonly List<VisitorVersion> _versions = [];

    private Visitor()
    {
    }

    internal Visitor(Guid requestId, int sequence, IEnumerable<VisitDay> visitDays, bool internalVisitor)
    {
        RequestId = requestId;
        Sequence = sequence;
        DetailsStatus = VisitorDetailsStatus.Draft;
        ScreeningDecision = internalVisitor ? ScreeningDecision.NotApplicable : ScreeningDecision.Pending;
        foreach (var day in visitDays)
        {
            _receptionRecords.Add(new ReceptionRecord(Id, day.Id));
        }
    }

    public Guid RequestId { get; private set; }
    public int Sequence { get; private set; }
    public VisitorDetailsStatus DetailsStatus { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string MiddleName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string Citizenship { get; private set; } = string.Empty;
    public string Designation { get; private set; } = string.Empty;
    public string CompanyName { get; private set; } = string.Empty;
    public string CompanyAddress { get; private set; } = string.Empty;
    public string OfficeCity { get; private set; } = string.Empty;
    public string OfficeCountry { get; private set; } = string.Empty;
    public string PhoneCountry { get; private set; } = string.Empty;
    public string PhoneDialCode { get; private set; } = string.Empty;
    public string Telephone { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string IdType { get; private set; } = string.Empty;
    public string OtherIdType { get; private set; } = string.Empty;
    public ScreeningDecision ScreeningDecision { get; private set; }
    public VisitorClassification Classification { get; private set; }
    public string ScreeningReason { get; private set; } = string.Empty;
    public DateTimeOffset? ScreeningDecidedAt { get; private set; }
    public IReadOnlyCollection<DeclaredAsset> Assets => _assets.AsReadOnly();
    public IReadOnlyCollection<DpsDocument> DpsDocuments => _dpsDocuments.AsReadOnly();
    public IReadOnlyCollection<ReceptionRecord> ReceptionRecords => _receptionRecords.AsReadOnly();
    public IReadOnlyCollection<VisitorVersion> Versions => _versions.AsReadOnly();
    public DpsDocument? CurrentDpsDocument => _dpsDocuments.Where(document => document.DeletedAt is null).MaxBy(document => document.Version);

    internal void SubmitDetails(VisitorDetails details, string actorId, DateTimeOffset now)
    {
        details.Validate();
        FirstName = details.FirstName.Trim();
        MiddleName = details.MiddleName.Trim();
        LastName = details.LastName.Trim();
        FullName = details.FullName;
        Citizenship = details.Citizenship.Trim();
        Designation = details.Designation.Trim();
        CompanyName = details.CompanyName.Trim();
        CompanyAddress = details.CompanyAddress.Trim();
        OfficeCity = details.OfficeCity.Trim();
        OfficeCountry = details.OfficeCountry.Trim();
        PhoneCountry = details.PhoneCountry.Trim();
        PhoneDialCode = details.PhoneDialCode.Trim();
        Telephone = details.Telephone.Trim();
        Email = details.Email.Trim();
        IdType = details.IdType.Trim();
        OtherIdType = details.OtherIdType.Trim();
        _assets.Clear();
        _assets.AddRange(details.Assets.Select(asset => new DeclaredAsset(Id, asset.AssetType, asset.Description, asset.SerialNumber)));
        DetailsStatus = VisitorDetailsStatus.Submitted;
        var snapshot = JsonSerializer.Serialize(details);
        _versions.Add(new VisitorVersion(Id, _versions.Count + 1, snapshot, actorId, now));
    }

    internal DpsDocument AttachDps(string blobName, string fileName, long size, string sha256, string actorId, DateTimeOffset now, bool replacementAllowed)
    {
        if (CurrentDpsDocument is not null && !replacementAllowed)
        {
            throw new DomainException("The DPS PDF can only be replaced while a correction is requested.");
        }

        CurrentDpsDocument?.MarkDeleted(now);
        var document = new DpsDocument(Id, blobName, fileName, size, sha256, actorId, now, _dpsDocuments.Count + 1);
        _dpsDocuments.Add(document);
        return document;
    }

    internal void RequestRevision()
    {
        DetailsStatus = VisitorDetailsStatus.RevisionRequired;
        ResetScreening();
    }

    internal void Approve(VisitorClassification classification, string reason, DateTimeOffset now)
    {
        if (classification == VisitorClassification.None)
        {
            throw new DomainException("A visitor classification is required.");
        }

        if (CurrentDpsDocument is null)
        {
            throw new DomainException("A DPS PDF is required before an external visitor can be approved.");
        }

        ScreeningDecision = ScreeningDecision.Approved;
        Classification = classification;
        ScreeningReason = reason.Trim();
        ScreeningDecidedAt = now;
    }

    internal void Reject(VisitorClassification classification, string reason, DateTimeOffset now)
    {
        if (classification == VisitorClassification.None || string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("A classification and rejection reason are required.");
        }

        if (CurrentDpsDocument is null)
        {
            throw new DomainException("A DPS PDF is required before an external visitor can be rejected.");
        }

        ScreeningDecision = ScreeningDecision.Rejected;
        Classification = classification;
        ScreeningReason = reason.Trim();
        ScreeningDecidedAt = now;
        foreach (var record in _receptionRecords.Where(record => record.Status == ReceptionStatus.Upcoming))
        {
            record.RejectEntry(reason);
        }
    }

    internal void AutoApprove(DateTimeOffset now)
    {
        ScreeningDecision = ScreeningDecision.NotApplicable;
        Classification = VisitorClassification.Visitor;
        ScreeningReason = string.Empty;
        ScreeningDecidedAt = now;
    }

    internal void ResetScreening()
    {
        ScreeningDecision = ScreeningDecision.Pending;
        Classification = VisitorClassification.None;
        ScreeningReason = string.Empty;
        ScreeningDecidedAt = null;
    }

    internal void ResetForRequestRevision()
    {
        ResetScreening();
        foreach (var record in _receptionRecords)
        {
            record.Reset();
        }

        foreach (var asset in _assets)
        {
            asset.Reset();
        }
    }

    internal void ReplaceReceptionSchedule(IEnumerable<VisitDay> visitDays)
    {
        if (_receptionRecords.Any(record => record.Status is ReceptionStatus.CheckedIn or ReceptionStatus.Completed))
        {
            throw new DomainException("A visitor cannot be rescheduled after check-in.");
        }

        _receptionRecords.Clear();
        _receptionRecords.AddRange(visitDays.Select(day => new ReceptionRecord(Id, day.Id)));
    }

    internal ReceptionRecord ReceptionRecord(Guid visitDayId) =>
        _receptionRecords.SingleOrDefault(record => record.VisitDayId == visitDayId)
        ?? throw new DomainException("The visitor is not scheduled for the selected day.");
}
