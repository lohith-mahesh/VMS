using System.ComponentModel.DataAnnotations;

namespace RRVMS.Api.DTOs;

public sealed class AttendanceUpdateDto
{
    [Required] public string Category { get; init; } = string.Empty;
    public Guid? VisitDayId { get; init; }
    public bool Completed { get; init; }
    [StringLength(1000)] public string? Comments { get; init; }
}