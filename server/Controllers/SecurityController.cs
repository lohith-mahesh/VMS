using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RRVMS.Api.Data;
using RRVMS.Api.Models;
using RRVMS.Api.Services;

namespace RRVMS.Api.Controllers;

[ApiController]
[Route("api/reception")]
public sealed class ReceptionController(RrvmsDbContext dbContext, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet("visitors")]
    public async Task<IActionResult> GetVisitors([FromQuery] string? search, [FromQuery] DateOnly? date, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || (currentUser.Role != "RECEPTION" && currentUser.Role != "EXPORT_CONTROL" && currentUser.Role != "HOST_REQUESTER")) return StatusCode(StatusCodes.Status403Forbidden);
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var query = dbContext.VisitDays.AsNoTracking()
            .Include(day => day.VisitorRequest)
            .ThenInclude(request => request.Visitor)
            .Include(day => day.VisitorRequest)
            .ThenInclude(request => request.Assets)
            .Where(day => day.VisitorRequest.Status == RequestStatus.APPROVED || day.Status == VisitDayStatus.CHECKED_IN || day.Status == VisitDayStatus.RECEPTION_HOLD || day.Status == VisitDayStatus.RECEPTION_VERIFICATION);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(day => day.VisitorRequest.Visitor.FullName.Contains(search) || day.VisitorRequest.Visitor.CompanyName.Contains(search) || day.VisitorRequest.RequestNumber.Contains(search) || day.VisitorRequest.BatchId.Contains(search));
        }

        var items = await query.OrderBy(day => day.VisitDate).Select(day => new
        {
            day.Id,
            day.VisitDate,
            status = day.Status.ToString(),
            requestId = day.VisitorRequestId,
            requestNumber = day.VisitorRequest.RequestNumber,
            batchId = string.IsNullOrWhiteSpace(day.VisitorRequest.BatchId) ? $"BATCH-2026-{day.VisitorRequest.RequestNumber.Replace("RRVMS-2026-", "")}" : day.VisitorRequest.BatchId,
            visitorName = day.VisitorRequest.Visitor.FullName,
            company = day.VisitorRequest.Visitor.CompanyName,
            idType = day.VisitorRequest.Visitor.IdType,
            assets = day.VisitorRequest.Assets.Select(a => new { a.Id, a.AssetType, a.Description, a.SerialNumber, verificationStatus = a.VerificationStatus.ToString() }).ToList()
        }).ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || (currentUser.Role != "RECEPTION" && currentUser.Role != "EXPORT_CONTROL" && currentUser.Role != "HOST_REQUESTER")) return StatusCode(StatusCodes.Status403Forbidden);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var todaysVisitsQuery = dbContext.VisitDays.AsNoTracking()
            .Include(day => day.VisitorRequest)
            .ThenInclude(request => request.Visitor)
            .Include(day => day.VisitorRequest)
            .ThenInclude(request => request.Assets)
            .Where(day => day.VisitDate == today && (day.VisitorRequest.Status == RequestStatus.APPROVED || day.Status == VisitDayStatus.CHECKED_IN || day.Status == VisitDayStatus.RECEPTION_HOLD || day.Status == VisitDayStatus.RECEPTION_VERIFICATION || day.Status == VisitDayStatus.COMPLETED));

        var todaysVisitors = await todaysVisitsQuery.CountAsync(cancellationToken);
        var expected = await dbContext.VisitDays.CountAsync(day => day.VisitDate == today && day.Status == VisitDayStatus.UPCOMING && day.VisitorRequest.Status == RequestStatus.APPROVED, cancellationToken);
        var arrived = await dbContext.VisitDays.CountAsync(day => day.VisitDate == today && (day.Status == VisitDayStatus.RECEPTION_VERIFICATION || day.Status == VisitDayStatus.CHECKED_IN), cancellationToken);
        var onHold = await dbContext.VisitDays.CountAsync(day => day.Status == VisitDayStatus.RECEPTION_HOLD, cancellationToken);
        var currentlyInside = await dbContext.VisitDays.CountAsync(day => day.Status == VisitDayStatus.CHECKED_IN, cancellationToken);
        var checkedOut = await dbContext.VisitDays.CountAsync(day => day.VisitDate == today && day.Status == VisitDayStatus.COMPLETED, cancellationToken);
        var noShow = await dbContext.VisitDays.CountAsync(day => day.VisitDate == today && day.Status == VisitDayStatus.NO_SHOW, cancellationToken);

        var items = await todaysVisitsQuery.OrderBy(day => day.VisitDate).Select(day => new
        {
            day.Id,
            day.VisitDate,
            status = day.Status.ToString(),
            requestId = day.VisitorRequestId,
            requestNumber = day.VisitorRequest.RequestNumber,
            batchId = string.IsNullOrWhiteSpace(day.VisitorRequest.BatchId) ? $"BATCH-2026-{day.VisitorRequest.RequestNumber.Replace("RRVMS-2026-", "")}" : day.VisitorRequest.BatchId,
            visitorName = day.VisitorRequest.Visitor.FullName,
            company = day.VisitorRequest.Visitor.CompanyName,
            idType = day.VisitorRequest.Visitor.IdType,
            assets = day.VisitorRequest.Assets.Select(a => new { a.Id, a.AssetType, a.Description, a.SerialNumber, verificationStatus = a.VerificationStatus.ToString() }).ToList()
        }).ToListAsync(cancellationToken);

        return Ok(new
        {
            todaysVisitors,
            expected,
            arrived,
            onHold,
            currentlyInside,
            checkedOut,
            noShow,
            items
        });
    }
}
