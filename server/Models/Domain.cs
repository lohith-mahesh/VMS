namespace RRVMS.Api.Models;

public enum UserRole { Requester, Host, ExportControl, Security, Admin }
public enum VisitorType { Internal, External }
public enum VisitDayStatus { Approved, Expected, Arrived, VerificationInProgress, OnHold, CheckedIn, CheckedOut, NoShow, Completed, Cancelled }
public enum AssetVerificationStatus { NotVerified, Verified, Mismatch, Undeclared, Rejected }
public enum DpsPerformedByType { Host, ExportControl }
public enum DpsStatus { NotRequired, Pending, InProgress, Completed, Failed }
public enum DpsResult { Clear, Flagged, NotApplicable }
public enum EcReviewStatus { Pending, InProgress, PendingDocumentation, Approved, Rejected }
public enum EcDecision { Approve, RequestDocumentation, Reject }
public enum BadgeStatus { Available, Issued, Returned, Lost }

public sealed class User
{
    public Guid Id { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class Visitor
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Citizenship { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string IdType { get; set; } = string.Empty;
    public string IdLast4 { get; set; } = string.Empty;
    public VisitorType VisitorType { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<VisitorRequest> Requests { get; set; } = [];
}

public sealed class VisitorRequest
{
    public Guid Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public Guid VisitorId { get; set; }
    public Guid RequesterId { get; set; }
    public Guid MainHostId { get; set; }
    public Guid? AccompanyingEmployeeId { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public string VisitingCompany { get; set; } = string.Empty;
    public string VisitingSite { get; set; } = string.Empty;
    public string VisitPurposeType { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? RejectionReason { get; set; }
    public string CurrentStatus { get; set; } = "Draft";
    public Visitor Visitor { get; set; } = null!;
    public ICollection<VisitDay> VisitDays { get; set; } = [];
    public ICollection<Asset> Assets { get; set; } = [];
    public ICollection<DPSRecord> DpsRecords { get; set; } = [];
    public ICollection<ECReview> EcReviews { get; set; } = [];
    public ICollection<Document> Documents { get; set; } = [];
}

public sealed class VisitDay
{
    public Guid Id { get; set; }
    public Guid VisitorRequestId { get; set; }
    public DateOnly VisitDate { get; set; }
    public TimeOnly? ExpectedArrivalTime { get; set; }
    public TimeOnly? ExpectedDepartureTime { get; set; }
    public VisitDayStatus Status { get; set; }
    public DateTimeOffset? ActualArrivalTime { get; set; }
    public DateTimeOffset? ActualDepartureTime { get; set; }
    public DateTimeOffset? NoShowMarkedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public VisitorRequest VisitorRequest { get; set; } = null!;
}

public sealed class Asset
{
    public Guid Id { get; set; }
    public Guid VisitorRequestId { get; set; }
    public string AssetType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public bool IsDeclared { get; set; }
    public bool IsVerified { get; set; }
    public AssetVerificationStatus VerificationStatus { get; set; }
    public DateTimeOffset? DetectedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public VisitorRequest VisitorRequest { get; set; } = null!;
}

public sealed class DPSRecord
{
    public Guid Id { get; set; }
    public Guid VisitorRequestId { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public DpsPerformedByType PerformedByType { get; set; }
    public DpsStatus Status { get; set; }
    public DpsResult Result { get; set; }
    public string? OCRReference { get; set; }
    public Guid? ReportDocumentId { get; set; }
    public DateTimeOffset? PerformedAt { get; set; }
    public string? Notes { get; set; }
    public VisitorRequest VisitorRequest { get; set; } = null!;
}

public sealed class ECReview
{
    public Guid Id { get; set; }
    public Guid VisitorRequestId { get; set; }
    public Guid ReviewerId { get; set; }
    public EcReviewStatus Status { get; set; }
    public EcDecision Decision { get; set; }
    public string Comments { get; set; } = string.Empty;
    public string RequestedDocuments { get; set; } = string.Empty;
    public DateTimeOffset? ReviewedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public VisitorRequest VisitorRequest { get; set; } = null!;
}

public sealed class Document
{
    public Guid Id { get; set; }
    public Guid VisitorRequestId { get; set; }
    public Guid? ECReviewId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string StorageReference { get; set; } = string.Empty;
    public Guid UploadedByUserId { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    public string Status { get; set; } = "Uploaded";
    public VisitorRequest VisitorRequest { get; set; } = null!;
}

public sealed class Badge
{
    public Guid Id { get; set; }
    public string BadgeNumber { get; set; } = string.Empty;
    public Guid VisitorId { get; set; }
    public Guid VisitDayId { get; set; }
    public Guid IssuedByUserId { get; set; }
    public DateTimeOffset IssuedAt { get; set; }
    public DateTimeOffset? ReturnedAt { get; set; }
    public BadgeStatus Status { get; set; }
}

public sealed class VisitCheckIn
{
    public Guid Id { get; set; }
    public Guid VisitDayId { get; set; }
    public Guid BadgeId { get; set; }
    public Guid SecurityUserId { get; set; }
    public bool PhysicalIdVerified { get; set; }
    public bool AssetsVerified { get; set; }
    public DateTimeOffset CheckedInAt { get; set; }
}

public sealed class VisitCheckOut
{
    public Guid Id { get; set; }
    public Guid VisitDayId { get; set; }
    public Guid BadgeId { get; set; }
    public Guid SecurityUserId { get; set; }
    public bool BadgeReturned { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTimeOffset CheckedOutAt { get; set; }
}

public sealed class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class AuditLog
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public string Details { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
