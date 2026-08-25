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
    [HttpPut]
    public async Task<IActionResult> Update(Guid requestId, AttendanceUpdateDto input, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.Role != "EXPORT_CONTROL") return Forbid();
        if (!Enum.TryParse<AttendanceCategory>(input.Category, true, out var category)) return BadRequest(new { error = "Invalid attendance category." });
        if (!await db.VisitorRequests.AnyAsync(request => request.Id == requestId && request.Status == RequestStatus.APPROVED, cancellationToken)) return Conflict(new { error = "Attendance is available only for approved requests." });
        var now = DateTimeOffset.UtcNow; var record = await db.AttendanceRecords.FirstOrDefaultAsync(item => item.VisitorRequestId == requestId && item.VisitDayId == input.VisitDayId && item.Category == category, cancellationToken);
        if (record is null) { record = new AttendanceRecord { Id = Guid.NewGuid(), VisitorRequestId = requestId, VisitDayId = input.VisitDayId, Category = category }; db.AttendanceRecords.Add(record); }
        record.Completed = input.Completed; record.MarkedByUserId = StableGuid(currentUser.UserId); record.MarkedAt = now; record.Comments = input.Comments;
        db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), Action = "ATTENDANCE_UPDATED", EntityType = nameof(VisitorRequest), EntityId = requestId, PerformedByUserId = record.MarkedByUserId, Details = $"{category}: {input.Completed}", CreatedAt = now }); await db.SaveChangesAsync(cancellationToken); return Ok(new AttendanceDto(record.Id, record.VisitDayId, record.Category.ToString(), record.Completed, record.MarkedByUserId, record.MarkedAt, record.Comments));
    }

    private static Guid StableGuid(string value) => new(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(value)));
}