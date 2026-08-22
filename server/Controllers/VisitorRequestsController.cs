using Microsoft.AspNetCore.Mvc;
using RRVMS.Api.DTOs;
using RRVMS.Api.Services;

namespace RRVMS.Api.Controllers;

[ApiController]
[Route("api/visitor-requests")]
public sealed class VisitorRequestsController(IVisitorRequestService service, ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await service.ListAsync(page, pageSize, cancellationToken);
        return Ok(new { items = result.Items, page, pageSize, total = result.Total });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(id, cancellationToken);
        return result is null ? NotFound(new { error = "Visitor request was not found." }) : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateVisitorRequestDto input, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated) return Unauthorized();
        try
        {
            var result = await service.CreateAsync(input, currentUserService.UserId, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }
}
