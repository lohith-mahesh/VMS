using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RRVMS.Api.Data;
using RRVMS.Api.Models;
using RRVMS.Api.Services;

namespace RRVMS.Api.Controllers;

[ApiController]
[Route("api/security/visitors")]
public sealed class SecurityController(RrvmsDbContext dbContext, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? search, [FromQuery] DateOnly? date, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || (currentUser.Role != "Security" && currentUser.Role != "Admin")) return StatusCode(StatusCodes.Status403Forbidden);
        var query = dbContext.VisitDays.AsNoTracking().Include(day => day.VisitorRequest).ThenInclude(request => request.Visitor).Where(day => day.Status == VisitDayStatus.Approved || day.Status == VisitDayStatus.CheckedIn || day.Status == VisitDayStatus.OnHold);
        if (date is not null) query = query.Where(day => day.VisitDate == date);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(day => day.VisitorRequest.Visitor.FullName.Contains(search) || day.VisitorRequest.Visitor.CompanyName.Contains(search) || day.VisitorRequest.RequestNumber.Contains(search));
        return Ok(await query.OrderBy(day => day.VisitDate).Select(day => new { day.Id, day.VisitDate, day.Status, requestId = day.VisitorRequestId, day.VisitorRequest.RequestNumber, visitorName = day.VisitorRequest.Visitor.FullName, company = day.VisitorRequest.Visitor.CompanyName }).ToListAsync(cancellationToken));
    }
}
