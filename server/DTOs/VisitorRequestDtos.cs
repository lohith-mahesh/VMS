using System.ComponentModel.DataAnnotations;

namespace RRVMS.Api.DTOs;

public sealed class CreateVisitorRequestDto
{
    [Required, StringLength(160)] public string FullName { get; init; } = string.Empty;
    [Required, StringLength(160)] public string CompanyName { get; init; } = string.Empty;
    [Required, StringLength(80)] public string Citizenship { get; init; } = string.Empty;
    [Required, StringLength(80)] public string Country { get; init; } = string.Empty;
    [StringLength(120)] public string Designation { get; init; } = string.Empty;
    [Required, EmailAddress] public string Email { get; init; } = string.Empty;
    [Required, StringLength(40)] public string Phone { get; init; } = string.Empty;
    [Required, StringLength(40)] public string IdType { get; init; } = string.Empty;
    [Required, RegularExpression("^[0-9]{4}$")] public string IdLast4 { get; init; } = string.Empty;
    [Required, StringLength(160)] public string VisitingCompany { get; init; } = string.Empty;
    [Required, StringLength(120)] public string VisitingSite { get; init; } = string.Empty;
    [Required, StringLength(1000)] public string Purpose { get; init; } = string.Empty;
    [Required, StringLength(80)] public string VisitPurposeType { get; init; } = string.Empty;
    [MinLength(1)] public List<CreateVisitDayDto> VisitDays { get; init; } = [];
    public List<CreateAssetDto> Assets { get; init; } = [];
}

public sealed class CreateVisitDayDto
{
    public DateOnly VisitDate { get; init; }
    public TimeOnly? ExpectedArrivalTime { get; init; }
    public TimeOnly? ExpectedDepartureTime { get; init; }
}

public sealed class CreateAssetDto
{
    [Required, StringLength(80)] public string AssetType { get; init; } = string.Empty;
    [StringLength(300)] public string Description { get; init; } = string.Empty;
    [StringLength(120)] public string SerialNumber { get; init; } = string.Empty;
}

public sealed record VisitorRequestListItemDto(Guid Id, string RequestNumber, string VisitorName, string CompanyName, string CurrentStatus, DateTimeOffset CreatedAt);
public sealed record VisitorRequestDetailDto(Guid Id, string RequestNumber, VisitorDto Visitor, string Purpose, string VisitingCompany, string VisitingSite, string VisitPurposeType, string CurrentStatus, IReadOnlyList<VisitDayDto> VisitDays, IReadOnlyList<AssetDto> Assets, IReadOnlyList<AuditDto> AuditHistory);
public sealed record VisitorDto(Guid Id, string FullName, string CompanyName, string Citizenship, string Country, string Designation, string Email, string Phone, string IdType, string IdLast4, string VisitorType);
public sealed record VisitDayDto(Guid Id, DateOnly VisitDate, TimeOnly? ExpectedArrivalTime, TimeOnly? ExpectedDepartureTime, string Status, DateTimeOffset? ActualArrivalTime, DateTimeOffset? ActualDepartureTime);
public sealed record AssetDto(Guid Id, string AssetType, string Description, string SerialNumber, bool IsDeclared, bool IsVerified, string VerificationStatus);
public sealed record AuditDto(Guid Id, string Action, string EntityType, Guid EntityId, string Details, DateTimeOffset CreatedAt);
