using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RRVMS.Api.Data;
using RRVMS.Api.Models;

namespace RRVMS.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(RrvmsDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var totalRequests = await dbContext.VisitorRequests.CountAsync(cancellationToken);
        var pendingEcReviews = await dbContext.VisitorRequests.CountAsync(request => request.Status == RequestStatus.EC_REVIEW, cancellationToken);
        var pendingDocumentation = await dbContext.VisitorRequests.CountAsync(request => request.Status == RequestStatus.PENDING_DOCUMENTATION, cancellationToken);
        var todaysVisits = await dbContext.VisitDays.CountAsync(day => day.VisitDate == today, cancellationToken);
        var currentlyInside = await dbContext.VisitDays.CountAsync(day => day.Status == VisitDayStatus.CHECKED_IN, cancellationToken);
        var noShows = await dbContext.VisitDays.CountAsync(day => day.Status == VisitDayStatus.NO_SHOW, cancellationToken);
        var recentRequests = await dbContext.VisitorRequests.AsNoTracking().Include(request => request.Visitor).OrderByDescending(request => request.UpdatedAt).Take(10).Select(request => new { request.Id, request.RequestNumber, visitorName = request.Visitor.FullName, currentStatus = request.Status.ToString(), createdAt = request.CreatedAt }).ToListAsync(cancellationToken);
        return Ok(new { totalRequests, pendingActions = pendingEcReviews + pendingDocumentation, todaysVisits, currentlyInside, upcomingVisits = await dbContext.VisitDays.CountAsync(day => day.VisitDate > today && day.VisitorRequest.Status == RequestStatus.APPROVED, cancellationToken), noShows, pendingEcReviews, pendingDocumentation, recentRequests });
    }
}

[ApiController]
[Route("api/users")]
public sealed class UsersController(RrvmsDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) => Ok(await dbContext.Users.AsNoTracking().Where(user => user.IsActive).OrderBy(user => user.FullName).Select(user => new { user.Id, user.EmployeeNumber, user.FullName, user.Email, role = user.Role.ToString() }).ToListAsync(cancellationToken));
}
