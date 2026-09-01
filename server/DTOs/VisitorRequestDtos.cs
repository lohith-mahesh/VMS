using System.ComponentModel.DataAnnotations;

namespace RRVMS.Api.DTOs;

public sealed class CreateVisitorRequestDto
{
    [Required] public string VisitorType { get; init; } = "External";
    [Required, StringLength(160)] public string VisitingCompany { get; init; } = string.Empty;
    [Required, StringLength(300)] public string VisitingCompanyAddressCountry { get; init; } = string.Empty;
    [Required, StringLength(120)] public string VisitingSite { get; init; } = string.Empty;
    [Required, StringLength(1000)] public string AreasToVisit { get; init; } = string.Empty;
    [Required, StringLength(80)] public string SiteTimezone { get; init; } = "Asia/Kolkata";
    [Range(1, 1000)] public int NumberOfVisitors { get; init; } = 1;
    [Required, StringLength(80)] public string VisitPurposeType { get; init; } = "Technical";
    [Required, StringLength(1000)] public string Purpose { get; init; } = string.Empty;
    [Required] public string MainHostId { get; init; } = string.Empty;
    public string? EscortingHostId { get; init; }
    [MinLength(1)] public List<CreateVisitDayDto> VisitDays { get; init; } = [];
}

public sealed class CreateVisitDayDto
{
    public DateOnly VisitDate { get; init; }
    public TimeOnly? ExpectedArrivalTime { get; init; }
    public TimeOnly? ExpectedDepartureTime { get; init; }
}

public sealed class VisitorFormDto
{
    public Guid Id { get; init; }
    public Guid VisitorRequestId { get; init; }
    public string RequestNumber { get; init; } = string.Empty;
    public string Status { get; init; } = "PENDING";
    public string FullName { get; init; } = string.Empty;
    public string Citizenship { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string Designation { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string OfficeCity { get; init; } = string.Empty;
    public string OfficeCountry { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Telephone { get; init; } = string.Empty;
    public string IdType { get; init; } = string.Empty;
    public string OtherIdType { get; init; } = string.Empty;
    public List<VisitorAssetDto> Assets { get; init; } = [];
}

public sealed class SubmitVisitorFormDto
{
    [Required, StringLength(160)] public string FullName { get; init; } = string.Empty;
    [Required, StringLength(80)] public string Citizenship { get; init; } = string.Empty;
    [Required, StringLength(80)] public string Country { get; init; } = string.Empty;
    [StringLength(120)] public string Designation { get; init; } = string.Empty;
    [Required, StringLength(160)] public string CompanyName { get; init; } = string.Empty;
    [StringLength(160)] public string OfficeCity { get; init; } = string.Empty;
    [Required, StringLength(80)] public string OfficeCountry { get; init; } = string.Empty;
    [Required, EmailAddress] public string Email { get; init; } = string.Empty;
    [Required, StringLength(40)] public string Telephone { get; init; } = string.Empty;
    [Required, StringLength(40)] public string IdType { get; init; } = string.Empty;
    [StringLength(80)] public string OtherIdType { get; init; } = string.Empty;
    public List<VisitorAssetDto> Assets { get; init; } = [];
}

public sealed class VisitorAssetDto
{
    [Required, StringLength(80)] public string AssetType { get; init; } = string.Empty;
    [StringLength(300)] public string Description { get; init; } = string.Empty;
    [StringLength(120)] public string SerialNumber { get; init; } = string.Empty;
}

public sealed record VisitorRequestListItemDto(
    Guid Id,
    string RequestNumber,
    string BatchId,
    string VisitorName,
    string CompanyName,
    string CurrentStatus,
    DateTimeOffset CreatedAt,
    DateOnly? VisitDate = null,
    string? HostName = null,
    string? DpsStatus = null,
    string? CurrentStage = null,
    DateTimeOffset? LastUpdated = null
);

public sealed record PreviousRequestDto(Guid Id, string RequestNumber, string VisitingSite, string Purpose, string CurrentStatus, DateTimeOffset CreatedAt);
public sealed record PreviousVisitDayDto(Guid Id, string RequestNumber, DateOnly VisitDate, string Status);

public sealed record VisitorRequestDetailDto(
    Guid Id,
    string RequestNumber,
    string BatchId,
    VisitorDto Visitor,
    string Purpose,
    string AreasToVisit,
    string VisitingCompany,
    string VisitingCompanyAddressCountry,
    string VisitingSite,
    string VisitPurposeType,
    string MainHostName,
    string? EscortingHostName,
    string CurrentStatus,
    IReadOnlyList<VisitDayDto> VisitDays,
    IReadOnlyList<AssetDto> Assets,
    IReadOnlyList<AuditDto> AuditHistory,
    Guid? VisitorFormId,
    IReadOnlyList<Guid> VisitorFormIds,
    IReadOnlyList<VisitorFormSummaryDto> VisitorForms,
    IReadOnlyList<DpsDto> DpsHistory,
    IReadOnlyList<EcReviewDto> EcReviews,
    IReadOnlyList<CommentDto> Comments,
    IReadOnlyList<InformationRequestDto> InformationRequests,
    IReadOnlyList<AttendanceDto> Attendance,
    IReadOnlyList<VisitorFormVersionDto> FormVersions,
    IReadOnlyList<PreviousRequestDto> PreviousRequests,
    IReadOnlyList<PreviousVisitDayDto> PreviousVisitDays
);

public sealed record VisitorFormSummaryDto(Guid Id, string Status, string FullName);
public sealed record VisitorDto(Guid Id, string FullName, string CompanyName, string Citizenship, string Country, string Designation, string Email, string Phone, string IdType, string OtherIdType, string VisitorType);
public sealed record VisitDayDto(Guid Id, DateOnly VisitDate, TimeOnly? ExpectedArrivalTime, TimeOnly? ExpectedDepartureTime, string Status, DateTimeOffset? ActualArrivalTime, DateTimeOffset? ActualDepartureTime);
public sealed record AssetDto(Guid Id, string AssetType, string Description, string SerialNumber, bool IsDeclared, bool IsVerified, string VerificationStatus);
public sealed record AuditDto(Guid Id, string Action, string EntityType, Guid EntityId, string Details, DateTimeOffset CreatedAt);
public sealed record DpsDto(Guid Id, string PerformedBy, string Status, string Result, string? Notes, DateTimeOffset? PerformedAt);
public sealed record EcReviewDto(Guid Id, Guid ReviewerId, string Status, string Decision, string Comments, DateTimeOffset? ReviewedAt);
public sealed record CommentDto(Guid Id, Guid AuthorId, string Type, string Text, DateTimeOffset CreatedAt);
public sealed record InformationRequestDto(Guid Id, string Fields, string Comment, string Status, DateTimeOffset CreatedAt, DateTimeOffset? RespondedAt, string? ResponseSummary);
public sealed record AttendanceDto(Guid Id, Guid? VisitDayId, string Category, bool Completed, Guid? MarkedByUserId, DateTimeOffset? MarkedAt, string? Comments);
public sealed record VisitorFormVersionDto(Guid Id, int Version, string FullName, string Citizenship, string Country, string Company, string Designation, string IdType, string OtherIdType, string Assets, DateTimeOffset CreatedAt);
