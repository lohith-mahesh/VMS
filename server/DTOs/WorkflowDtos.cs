using System.ComponentModel.DataAnnotations;

namespace RRVMS.Api.DTOs;

public sealed class WorkflowActionDto
{
    [Required] public string Action { get; init; } = string.Empty;
    public string? Comment { get; init; }
    public string? Reason { get; init; }
    public Guid? VisitDayId { get; init; }
    public string? BadgeNumber { get; init; }
    public string? IdLast4 { get; init; }
    public string? AssetSerials { get; init; }
    public string? NewUserId { get; init; }
}

public sealed record WorkflowResultDto(Guid RequestId, string RequestNumber, string Status, string Action);
