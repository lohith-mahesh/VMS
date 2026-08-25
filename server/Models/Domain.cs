namespace RRVMS.Api.Models;

// FINAL THREE APPLICATION ROLES
public enum UserRole
{
    HOST_REQUESTER,
    EXPORT_CONTROL,
    RECEPTION
}

public enum VisitorType { Internal, External }

// WORKFLOW STATE MACHINE - AUTHORITATIVE
public enum RequestStatus
{
    DRAFT,
    VISITOR_FORM_PENDING,
    VISITOR_FORM_SUBMITTED,
    HOST_REVIEW,
    HOST_FINAL_SUBMITTED,
    CANCELLED_PERSONNEL_CHANGE,
    HOST_DPS,
    EC_DPS,
    EC_REVIEW,
    PENDING_DOCUMENTATION,
    DOCUMENTATION_SUBMITTED,
    EC_RE_REVIEW_REQUIRED,
    APPROVED,
    REJECTED,
    RECEPTION_HOLD,
    VISIT_PROCESS_COMPLETED
}

public enum VisitDayStatus
{
    UPCOMING,
    NO_SHOW,
    RECEPTION_VERIFICATION,
    RECEPTION_HOLD,
    ENTRY_REJECTED,
    CHECKED_IN,
    CHECKED_OUT,
    COMPLETED
}

public enum AssetVerificationStatus { NotVerified, Verified, Mismatch, Undeclared, Rejected }
public enum DpsPerformedByType { HOST_REQUESTER, EXPORT_CONTROL }
public enum DpsStatus { NotRequired, Pending, InProgress, Completed, Failed }
public enum DpsResult { Clear, Flagged, Pending, Rejected }
public enum EcReviewStatus { Pending, InProgress, PendingDocumentation, Approved, Rejected }
public enum EcDecision { Approve, RequestDocumentation, Reject }
public enum BadgeStatus { Available, Issued, Returned, Lost }
public enum AttendanceCategory { FACILITIES_CONTRACTOR, GAS_TURBINE_RESEARCH_ESTABLISHMENT }
public enum CommentType { EC_REQUEST, EC_REJECTION, HOST_CHANGE, UNDECLARED_ASSET, HOLD, EXCEPTION, GENERAL }

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

    // Navigation
    public ICollection<VisitorRequest> CreatedRequests { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<ECReview> Reviews { get; set; } = [];
}

public sealed class Visitor
{
    public Guid Id { get; set; }
    public Guid? VisitorRequestId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Citizenship { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string IdType { get; set; } = string.Empty;
    public string IdLast4 { get; set; } = string.Empty;
    public VisitorType VisitorType { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation
    public ICollection<VisitorRequest> Requests { get; set; } = [];
    public ICollection<VisitorForm> Forms { get; set; } = [];
    public ICollection<Asset> Assets { get; set; } = [];
}

public sealed class VisitorRequest
{
    public Guid Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    
    // WORKFLOW STATE
    public RequestStatus Status { get; set; } = RequestStatus.DRAFT;
    
    // VISITOR AND REQUESTER
    public Guid VisitorId { get; set; }
    public Guid RequesterId { get; set; }
    
    // HOSTS
    public Guid MainHostId { get; set; }
    public Guid? PreviousMainHostId { get; set; }
    public DateTimeOffset? MainHostChangedAt { get; set; }
    public Guid? EscortingHostId { get; set; }
    
    // BASIC VISIT DETAILS (entered by Host/Requester at creation)
    public VisitorType VisitorType { get; set; }
    public string VisitingCompany { get; set; } = string.Empty;
    public string VisitingSite { get; set; } = string.Empty;
    public string VisitPurposeType { get; set; } = string.Empty; // Technical, Non-Technical, Other
    public string Purpose { get; set; } = string.Empty;
    public string AreasToVisit { get; set; } = string.Empty;
    public string SiteTimezone { get; set; } = string.Empty;
    public int NumberOfVisitors { get; set; }
    
    // VISITOR FORM
    public Guid? VisitorFormId { get; set; }
    
    // DPS
    public Guid? DpsRecordId { get; set; }
    public DpsPerformedByType? DpsPerformedBy { get; set; }
    
    // REJECTION INFO
    public string? RejectionReason { get; set; }
    public DateTimeOffset? RejectedAt { get; set; }
    
    // PERSONNEL CHANGE
    public bool PersonnelChangeRequested { get; set; }
    public DateTimeOffset? PersonnelChangeRequestedAt { get; set; }
    
    // TIMESTAMPS
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    
    // NAVIGATION
    public Visitor Visitor { get; set; } = null!;
    public ICollection<VisitorForm> VisitorForms { get; set; } = [];
    public ICollection<VisitDay> VisitDays { get; set; } = [];
    public ICollection<Asset> Assets { get; set; } = [];
    public ICollection<DPSRecord> DpsRecords { get; set; } = [];
    public ICollection<ECReview> EcReviews { get; set; } = [];
    public ICollection<Document> Documents { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<AdditionalInformationRequest> InformationRequests { get; set; } = [];
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = [];
}

public sealed class VisitDay
{
    public Guid Id { get; set; }
    public Guid VisitorRequestId { get; set; }
    public DateOnly VisitDate { get; set; }
    public TimeOnly? ExpectedArrivalTime { get; set; }
    public TimeOnly? ExpectedDepartureTime { get; set; }
    public VisitDayStatus Status { get; set; } = VisitDayStatus.UPCOMING;
    public DateTimeOffset? ActualArrivalTime { get; set; }
    public DateTimeOffset? ActualDepartureTime { get; set; }
    public DateTimeOffset? NoShowMarkedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    
    // Navigation
    public VisitorRequest VisitorRequest { get; set; } = null!;
}

public sealed class Asset
{
    public Guid Id { get; set; }
    public Guid VisitorRequestId { get; set; }
    public Guid? VisitorId { get; set; }
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
    public Visitor? Visitor { get; set; }
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
    
    // Navigation
    public VisitorRequest VisitorRequest { get; set; } = null!;
    public User Reviewer { get; set; } = null!;
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
    public Guid ReceptionUserId { get; set; }
    public bool PhysicalIdVerified { get; set; }
    public bool AssetsVerified { get; set; }
    public DateTimeOffset CheckedInAt { get; set; }
}

public sealed class VisitCheckOut
{
    public Guid Id { get; set; }
    public Guid VisitDayId { get; set; }
    public Guid BadgeId { get; set; }
    public Guid ReceptionUserId { get; set; }
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

// NEW MODELS FOR COMPLETE WORKFLOW

public sealed class VisitorForm
{
    public Guid Id { get; set; }
    public Guid VisitorRequestId { get; set; }
    public Guid VisitorId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Citizenship { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string OfficeCity { get; set; } = string.Empty;
    public string OfficeCountry { get; set; } = string.Empty;
    public string Telephone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string IdType { get; set; } = string.Empty;
    public string IdLast4 { get; set; } = string.Empty;
    public string DeclaredAssets { get; set; } = string.Empty;
    
    // STATUS TRACKING
    public string Status { get; set; } = "PENDING"; // PENDING, SUBMITTED, APPROVED, REJECTED
    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    
    // NAVIGATION
    public VisitorRequest VisitorRequest { get; set; } = null!;
    public Visitor Visitor { get; set; } = null!;
}

public sealed class VisitorFormVersion
{
    public Guid Id { get; set; }
    public Guid VisitorRequestId { get; set; }
    public Guid VisitorFormId { get; set; }
    public int Version { get; set; }
    public string FullNameSnapshot { get; set; } = string.Empty;
    public string CitizenshipSnapshot { get; set; } = string.Empty;
    public string NationalitySnapshot { get; set; } = string.Empty;
    public string CountrySnapshot { get; set; } = string.Empty;
    public string CompanySnapshot { get; set; } = string.Empty;
    public string OfficeCitySnapshot { get; set; } = string.Empty;
    public string OfficeCountrySnapshot { get; set; } = string.Empty;
    public string DesignationSnapshot { get; set; } = string.Empty;
    public string PhoneSnapshot { get; set; } = string.Empty;
    public string EmailSnapshot { get; set; } = string.Empty;
    public string IdTypeSnapshot { get; set; } = string.Empty;
    public string IdLast4Snapshot { get; set; } = string.Empty;
    public string AssetsSnapshot { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class Comment
{
    public Guid Id { get; set; }
    public Guid VisitorRequestId { get; set; }
    public Guid AuthorUserId { get; set; }
    public CommentType CommentType { get; set; }
    public string CommentText { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    
    // NAVIGATION
    public VisitorRequest VisitorRequest { get; set; } = null!;
    public User Author { get; set; } = null!;
}

public sealed class AdditionalInformationRequest
{
    public Guid Id { get; set; }
    public Guid VisitorRequestId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public Guid? VisitorFormId { get; set; }
    public string RequestedFields { get; set; } = string.Empty;
    public string RequestComment { get; set; } = string.Empty;
    public string Status { get; set; } = "PENDING"; // PENDING, SUBMITTED, APPROVED, REJECTED
    public DateTimeOffset? RespondedAt { get; set; }
    public string? ResponseSummary { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    
    // NAVIGATION
    public VisitorRequest VisitorRequest { get; set; } = null!;
    public User RequestedBy { get; set; } = null!;
    public VisitorForm? VisitorForm { get; set; }
}

public sealed class AttendanceRecord
{
    public Guid Id { get; set; }
    public Guid VisitorRequestId { get; set; }
    public Guid? VisitDayId { get; set; }
    public AttendanceCategory Category { get; set; }
    public bool Completed { get; set; }
    public Guid? MarkedByUserId { get; set; }
    public DateTimeOffset? MarkedAt { get; set; }
    public string? Comments { get; set; }
    
    // NAVIGATION
    public VisitorRequest VisitorRequest { get; set; } = null!;
    public VisitDay? VisitDay { get; set; }
}
