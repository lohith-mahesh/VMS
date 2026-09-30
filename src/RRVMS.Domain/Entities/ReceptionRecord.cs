using RRVMS.Domain.Common;
using RRVMS.Domain.Enums;

namespace RRVMS.Domain.Entities;

public sealed class ReceptionRecord : Entity
{
    private ReceptionRecord()
    {
    }

    internal ReceptionRecord(Guid visitorId, Guid visitDayId)
    {
        VisitorId = visitorId;
        VisitDayId = visitDayId;
        Status = ReceptionStatus.Upcoming;
        IdentityStatus = VerificationStatus.Pending;
        AssetsStatus = VerificationStatus.Pending;
    }

    public Guid VisitorId { get; private set; }
    public Guid VisitDayId { get; private set; }
    public ReceptionStatus Status { get; private set; }
    public VerificationStatus IdentityStatus { get; private set; }
    public VerificationStatus AssetsStatus { get; private set; }
    public ArrivalStatus ArrivalStatus { get; private set; }
    public string BadgeId { get; private set; } = string.Empty;
    public string BadgeType { get; private set; } = string.Empty;
    public string VerificationRemarks { get; private set; } = string.Empty;
    public DateTimeOffset? ActualArrival { get; private set; }
    public DateTimeOffset? ActualDeparture { get; private set; }
    public DateTimeOffset? BadgeReturnedAt { get; private set; }

    internal void ApproveVerification(bool hasAssets, string remarks)
    {
        EnsureStatus(ReceptionStatus.Upcoming);
        IdentityStatus = VerificationStatus.Approved;
        AssetsStatus = hasAssets ? VerificationStatus.Approved : VerificationStatus.NotApplicable;
        VerificationRemarks = remarks.Trim();
        Status = ReceptionStatus.VerificationComplete;
    }

    internal void RejectEntry(string remarks)
    {
        if (string.IsNullOrWhiteSpace(remarks))
        {
            throw new DomainException("Remarks are required when entry is rejected.");
        }

        EnsureStatus(ReceptionStatus.Upcoming);
        IdentityStatus = VerificationStatus.Rejected;
        AssetsStatus = VerificationStatus.Rejected;
        VerificationRemarks = remarks.Trim();
        Status = ReceptionStatus.EntryRejected;
    }

    internal void CheckIn(string badgeId, string badgeType, DateTimeOffset actualArrival, DateTimeOffset expectedArrival)
    {
        EnsureStatus(ReceptionStatus.VerificationComplete);
        if (string.IsNullOrWhiteSpace(badgeId))
        {
            throw new DomainException("A badge ID is required.");
        }

        BadgeId = badgeId.Trim();
        BadgeType = badgeType;
        ActualArrival = actualArrival;
        ArrivalStatus = actualArrival < expectedArrival.AddHours(-1)
            ? ArrivalStatus.Early
            : actualArrival > expectedArrival.AddHours(1)
                ? ArrivalStatus.Late
                : ArrivalStatus.OnTime;
        Status = ReceptionStatus.CheckedIn;
    }

    internal void CheckOut(DateTimeOffset departedAt)
    {
        EnsureStatus(ReceptionStatus.CheckedIn);
        ActualDeparture = departedAt;
        BadgeReturnedAt = departedAt;
        Status = ReceptionStatus.Completed;
    }

    internal void MarkNoShow()
    {
        EnsureStatus(ReceptionStatus.Upcoming);
        Status = ReceptionStatus.NoShow;
    }

    internal void Cancel()
    {
        if (Status is ReceptionStatus.CheckedIn or ReceptionStatus.Completed)
        {
            throw new DomainException("A started visit cannot be cancelled.");
        }

        Status = ReceptionStatus.Cancelled;
        BadgeId = string.Empty;
        BadgeType = string.Empty;
    }

    internal void Reset()
    {
        if (Status is ReceptionStatus.CheckedIn or ReceptionStatus.Completed)
        {
            throw new DomainException("A started reception record cannot be reset.");
        }

        Status = ReceptionStatus.Upcoming;
        IdentityStatus = VerificationStatus.Pending;
        AssetsStatus = VerificationStatus.Pending;
        ArrivalStatus = ArrivalStatus.NotArrived;
        BadgeId = string.Empty;
        BadgeType = string.Empty;
        VerificationRemarks = string.Empty;
        ActualArrival = null;
        ActualDeparture = null;
        BadgeReturnedAt = null;
    }

    private void EnsureStatus(ReceptionStatus expected)
    {
        if (Status != expected)
        {
            throw new DomainException($"The reception record must be {expected} for this operation.");
        }
    }
}
