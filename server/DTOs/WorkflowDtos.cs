using System.ComponentModel.DataAnnotations;

namespace RRVMS.Api.DTOs;

public sealed class WorkflowActionDto
{
    [Required] public string Action { get; init; } = string.Empty;
    public string? Comment { get; init; }
    public string? Reason { get; init; }
    public Guid? VisitDayId { get; init; }
    public string? BadgeNumber { get; init; }
    public string? BadgeColor { get; init; }
    public string? IdType { get; init; }
    public string? OtherIdType { get; init; }
    public string? AssetSerials { get; init; }
    public string? NewUserId { get; init; }
    public string? DpsResult { get; init; }
    public string? DpsNotes { get; init; }
    public string? DpsPerformer { get; init; }
    public bool? IdentityVerified { get; init; }
    public bool? AssetsVerified { get; init; }
    public string? ReceptionDecision { get; init; }
}

public sealed record WorkflowResultDto(Guid RequestId, string RequestNumber, string Status, string Action);
