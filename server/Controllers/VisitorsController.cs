using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RRVMS.Api.Data;
using RRVMS.Api.DTOs;

namespace RRVMS.Api.Controllers;

[ApiController]
[Route("api/visitors")]
public sealed class VisitorsController(RrvmsDbContext dbContext) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var visitor = await dbContext.Visitors.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (visitor is null) return NotFound(new { error = "Visitor was not found." });

        var previousRequests = await dbContext.VisitorRequests.AsNoTracking()
            .Where(r => r.VisitorId == id)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new PreviousRequestDto(r.Id, r.RequestNumber, r.VisitingSite, r.Purpose, r.Status.ToString(), r.CreatedAt))
            .ToListAsync(cancellationToken);

        var previousVisitDays = await dbContext.VisitDays.AsNoTracking()
            .Include(vd => vd.VisitorRequest)
            .Where(vd => vd.VisitorRequest.VisitorId == id)
            .OrderByDescending(vd => vd.VisitDate)
            .Select(vd => new PreviousVisitDayDto(vd.Id, vd.VisitorRequest.RequestNumber, vd.VisitDate, vd.Status.ToString()))
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            id = visitor.Id,
            fullName = visitor.FullName,
            companyName = visitor.CompanyName,
            citizenship = visitor.Citizenship,
            nationality = visitor.Nationality,
            country = visitor.Country,
            designation = visitor.Designation,
            email = visitor.Email,
            phone = visitor.Phone,
            idType = visitor.IdType,
            idLast4 = visitor.IdLast4,
            visitorType = visitor.VisitorType.ToString(),
            previousRequests,
            previousVisitDays
        });
    }
}
