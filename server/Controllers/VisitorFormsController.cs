using Microsoft.AspNetCore.Mvc;
using RRVMS.Api.DTOs;
using RRVMS.Api.Services;

namespace RRVMS.Api.Controllers;

[ApiController]
[Route("api/visitor-forms")]
public sealed class VisitorFormsController(IVisitorRequestService service) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) => (await service.GetVisitorFormAsync(id, cancellationToken)) is { } form ? Ok(form) : NotFound(new { error = "Visitor form was not found." });

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, VisitorFormDto input, CancellationToken cancellationToken)
    {
        try { await service.SubmitVisitorFormAsync(id, input, cancellationToken); return NoContent(); }
        catch (KeyNotFoundException exception) { return NotFound(new { error = exception.Message }); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { error = exception.Message }); }
    }
}