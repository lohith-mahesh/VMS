using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RRVMS.Api.Data;
using RRVMS.Api.DTOs;
using RRVMS.Api.Models;
using RRVMS.Api.Services;

namespace RRVMS.Api.Controllers;

[ApiController]
[Route("api/visitor-requests/{requestId:guid}/attendance")]
public sealed class AttendanceController(RrvmsDbContext db, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid requestId, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated) return Unauthorized();
        var records = await db.AttendanceRecords.AsNoTracking()
            .Where(r => r.VisitorRequestId == requestId)
            .Select(record => new AttendanceDto(record.Id, record.VisitDayId, record.Category.ToString(), record.Completed, record.MarkedByUserId, record.MarkedAt, record.Comments))
            .ToListAsync(cancellationToken);
        return Ok(records);
    }

    [HttpPut]
    public async Task<IActionResult> Update(Guid requestId, AttendanceUpdateDto input, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || (currentUser.Role != "EXPORT_CONTROL" && currentUser.Role != "HOST_REQUESTER")) return Forbid();
        if (!Enum.TryParse<AttendanceCategory>(input.Category, true, out var category)) return BadRequest(new { error = "Invalid attendance category." });
        
        var request = await db.VisitorRequests.FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);
        if (request is null) return NotFound(new { error = "Visitor request was not found." });
        if (request.Status != RequestStatus.APPROVED && request.Status != RequestStatus.VISIT_PROCESS_COMPLETED)
        {
            return Conflict(new { error = "Attendance is available only for approved requests." });
        }

        var now = DateTimeOffset.UtcNow;
        var record = await db.AttendanceRecords.FirstOrDefaultAsync(item => item.VisitorRequestId == requestId && item.VisitDayId == input.VisitDayId && item.Category == category, cancellationToken);
        if (record is null)
        {
            record = new AttendanceRecord { Id = Guid.NewGuid(), VisitorRequestId = requestId, VisitDayId = input.VisitDayId, Category = category };
            db.AttendanceRecords.Add(record);
        }
        record.Completed = input.Completed;
        record.MarkedByUserId = StableGuid(currentUser.UserId);
        record.MarkedAt = now;
        record.Comments = input.Comments;

        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            Action = "ATTENDANCE_UPDATED",
            EntityType = nameof(VisitorRequest),
            EntityId = requestId,
            PerformedByUserId = record.MarkedByUserId,
            Details = $"{category}: {input.Completed}",
            CreatedAt = now
        });

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new AttendanceDto(record.Id, record.VisitDayId, record.Category.ToString(), record.Completed, record.MarkedByUserId, record.MarkedAt, record.Comments));
    }

    private static Guid StableGuid(string value) => new(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(value)));
}

public sealed class AttendanceUpdateDto
{
    public Guid? VisitDayId { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool Completed { get; set; }
    public string? Comments { get; set; }
}