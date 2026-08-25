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
        var pendingEcReviews = await requests.CountAsync(request => request.Status == RequestStatus.EC_REVIEW, cancellationToken);
        var pendingDocumentation = await requests.CountAsync(request => request.Status == RequestStatus.PENDING_DOCUMENTATION, cancellationToken);
        var todaysVisits = await dbContext.VisitDays.CountAsync(day => day.VisitDate == today, cancellationToken);
        var currentlyInside = await dbContext.VisitDays.CountAsync(day => day.Status == VisitDayStatus.CHECKED_IN, cancellationToken);
        var noShows = await dbContext.VisitDays.CountAsync(day => day.Status == VisitDayStatus.NO_SHOW, cancellationToken);
        var recentRequests = await requests.AsNoTracking().Include(request => request.Visitor).OrderByDescending(request => request.UpdatedAt).Take(10).Select(request => new { request.Id, request.RequestNumber, visitorName = request.Visitor.FullName, companyName = request.VisitingCompany, currentStatus = request.Status.ToString(), createdAt = request.CreatedAt }).ToListAsync(cancellationToken);
        return Ok(new { totalRequests, pendingActions = pendingEcReviews + pendingDocumentation, todaysVisits, currentlyInside, upcomingVisits = await dbContext.VisitDays.CountAsync(day => day.VisitDate > today && day.VisitorRequest.Status == RequestStatus.APPROVED, cancellationToken), noShows, pendingEcReviews, pendingDocumentation, recentRequests });
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
