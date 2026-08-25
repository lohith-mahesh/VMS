using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RRVMS.Api.Data;
using RRVMS.Api.Models;
using RRVMS.Api.Services;

namespace RRVMS.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(RrvmsDbContext dbContext, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (!currentUser.IsAuthenticated) return Unauthorized();
        var role = currentUser.Role;
        var requests = dbContext.VisitorRequests.AsQueryable();
        if (role == "HOST_REQUESTER") requests = requests.Where(request => request.RequesterId == StableGuid(currentUser.UserId));
        var totalRequests = await requests.CountAsync(cancellationToken);
        var pendingEcReviews = await requests.CountAsync(request => request.Status == RequestStatus.EC_REVIEW || request.Status == RequestStatus.EC_RE_REVIEW_REQUIRED || request.Status == RequestStatus.EC_DPS, cancellationToken);
        var pendingDocumentation = await requests.CountAsync(request => request.Status == RequestStatus.PENDING_DOCUMENTATION, cancellationToken);
        var todaysVisits = await dbContext.VisitDays.CountAsync(day => day.VisitDate == today, cancellationToken);
        var currentlyInside = await dbContext.VisitDays.CountAsync(day => day.Status == VisitDayStatus.CHECKED_IN, cancellationToken);
        var noShows = await dbContext.VisitDays.CountAsync(day => day.Status == VisitDayStatus.NO_SHOW, cancellationToken);
        var recentRequests = await requests.AsNoTracking().Include(request => request.Visitor).OrderByDescending(request => request.UpdatedAt).Take(10).Select(request => new { request.Id, request.RequestNumber, visitorName = request.Visitor.FullName, companyName = request.VisitingCompany, currentStatus = request.Status.ToString(), createdAt = request.CreatedAt }).ToListAsync(cancellationToken);
        return Ok(new { totalRequests, pendingActions = pendingEcReviews + pendingDocumentation, todaysVisits, currentlyInside, upcomingVisits = await dbContext.VisitDays.CountAsync(day => day.VisitDate > today && day.VisitorRequest.Status == RequestStatus.APPROVED, cancellationToken), noShows, pendingEcReviews, pendingDocumentation, recentRequests });
    }

    [HttpGet("/api/ec/dashboard")]
    public async Task<IActionResult> GetEcDashboard(CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated) return Unauthorized();
        if (currentUser.Role != "EXPORT_CONTROL" && currentUser.Role != "HOST_REQUESTER" && currentUser.Role != "RECEPTION") return Unauthorized();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var requests = dbContext.VisitorRequests.AsNoTracking()
            .Include(r => r.Visitor)
            .Include(r => r.VisitDays)
            .Include(r => r.DpsRecords);

        var pendingEcReviewsList = await requests
            .Where(r => r.Status == RequestStatus.EC_REVIEW || r.Status == RequestStatus.EC_RE_REVIEW_REQUIRED || r.Status == RequestStatus.EC_DPS)
            .OrderByDescending(r => r.UpdatedAt)
            .Select(r => new {
                r.Id,
                r.RequestNumber,
                visitorName = r.Visitor.FullName,
                companyName = r.VisitingCompany,
                visitDate = r.VisitDays.Select(d => (DateOnly?)d.VisitDate).FirstOrDefault(),
                dpsStatus = r.DpsRecords.OrderByDescending(d => d.PerformedAt).Select(d => d.Result.ToString()).FirstOrDefault() ?? "Flagged",
                currentStage = "Export Control Review",
                currentStatus = r.Status.ToString(),
                lastUpdated = r.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        var pendingDocumentationList = await requests
            .Where(r => r.Status == RequestStatus.PENDING_DOCUMENTATION)
            .OrderByDescending(r => r.UpdatedAt)
            .Select(r => new {
                r.Id,
                r.RequestNumber,
                visitorName = r.Visitor.FullName,
                companyName = r.VisitingCompany,
                visitDate = r.VisitDays.Select(d => (DateOnly?)d.VisitDate).FirstOrDefault(),
                dpsStatus = r.DpsRecords.OrderByDescending(d => d.PerformedAt).Select(d => d.Result.ToString()).FirstOrDefault() ?? "Pending",
                currentStage = "Pending Documentation",
                currentStatus = r.Status.ToString(),
                lastUpdated = r.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        var dpsFlagsList = await requests
            .Where(r => r.DpsRecords.Any(d => d.Result == DpsResult.Flagged))
            .OrderByDescending(r => r.UpdatedAt)
            .Select(r => new {
                r.Id,
                r.RequestNumber,
                visitorName = r.Visitor.FullName,
                companyName = r.VisitingCompany,
                visitDate = r.VisitDays.Select(d => (DateOnly?)d.VisitDate).FirstOrDefault(),
                dpsStatus = "FLAGGED",
                currentStage = r.Status.ToString(),
                currentStatus = r.Status.ToString(),
                lastUpdated = r.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        var approvedCount = await dbContext.VisitorRequests.CountAsync(r => r.Status == RequestStatus.APPROVED || r.Status == RequestStatus.VISIT_PROCESS_COMPLETED, cancellationToken);
        var rejectedCount = await dbContext.VisitorRequests.CountAsync(r => r.Status == RequestStatus.REJECTED, cancellationToken);
        var visitorHistoryCount = await dbContext.VisitorRequests.CountAsync(r => r.Status == RequestStatus.VISIT_PROCESS_COMPLETED, cancellationToken);
        var attendanceCount = await dbContext.AttendanceRecords.CountAsync(cancellationToken);

        return Ok(new {
            pendingEcReviews = pendingEcReviewsList.Count,
            pendingDocumentation = pendingDocumentationList.Count,
            dpsFlags = dpsFlagsList.Count,
            approved = approvedCount,
            rejected = rejectedCount,
            visitorHistory = visitorHistoryCount,
            attendance = attendanceCount,
            pendingEcReviewsItems = pendingEcReviewsList,
            pendingDocumentationItems = pendingDocumentationList,
            dpsFlagsItems = dpsFlagsList
        });
    }

    private static Guid StableGuid(string value) => new(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(value)));
}

[ApiController]
[Route("api/users")]
public sealed class UsersController(RrvmsDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) => Ok(await dbContext.Users.AsNoTracking().Where(user => user.IsActive).OrderBy(user => user.FullName).Select(user => new { user.Id, user.EmployeeNumber, user.FullName, user.Email, role = user.Role.ToString() }).ToListAsync(cancellationToken));
}
