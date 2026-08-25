using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RRVMS.Api.Data;
using RRVMS.Api.Models;
using RRVMS.Api.Services;

namespace RRVMS.Api.Controllers;

[ApiController]
[Route("api/reception/visitors")]
public sealed class ReceptionController(RrvmsDbContext dbContext, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? search, [FromQuery] DateOnly? date, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.Role != "RECEPTION") return StatusCode(StatusCodes.Status403Forbidden);
        var query = dbContext.VisitDays.AsNoTracking().Include(day => day.VisitorRequest).ThenInclude(request => request.Visitor).Where(day => day.VisitorRequest.Status == RequestStatus.APPROVED || day.Status == VisitDayStatus.CHECKED_IN || day.Status == VisitDayStatus.RECEPTION_HOLD);
        if (date is not null) query = query.Where(day => day.VisitDate == date);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(day => day.VisitorRequest.Visitor.FullName.Contains(search) || day.VisitorRequest.Visitor.CompanyName.Contains(search) || day.VisitorRequest.RequestNumber.Contains(search));
        return Ok(await query.OrderBy(day => day.VisitDate).Select(day => new { day.Id, day.VisitDate, day.Status, requestId = day.VisitorRequestId, day.VisitorRequest.RequestNumber, visitorName = day.VisitorRequest.Visitor.FullName, company = day.VisitorRequest.Visitor.CompanyName }).ToListAsync(cancellationToken));
    }
}
