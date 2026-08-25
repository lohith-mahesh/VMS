using Microsoft.AspNetCore.Mvc;
using RRVMS.Api.DTOs;
using RRVMS.Api.Services;

namespace RRVMS.Api.Controllers;

[ApiController]
[Route("api/visitor-requests")]
[Route("api/requests")]
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

    [HttpGet("{id:guid}/history")]
    public async Task<IActionResult> GetHistory(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(id, cancellationToken);
        if (result is null) return NotFound(new { error = "Visitor request was not found." });
        return Ok(new { previousRequests = result.PreviousRequests, previousVisitDays = result.PreviousVisitDays, auditHistory = result.AuditHistory });
    }

    [HttpGet("{id:guid}/comments")]
    public async Task<IActionResult> GetComments(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(id, cancellationToken);
        if (result is null) return NotFound(new { error = "Visitor request was not found." });
        return Ok(result.Comments);
    }

    [HttpGet("{id:guid}/information-requests")]
    public async Task<IActionResult> GetInformationRequests(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(id, cancellationToken);
        if (result is null) return NotFound(new { error = "Visitor request was not found." });
        return Ok(result.InformationRequests);
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

    [HttpPost("{id:guid}/actions")]
    public async Task<IActionResult> Action(Guid id, WorkflowActionDto input, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated) return Unauthorized();
        try
        {
            var result = await service.ExecuteActionAsync(id, input, currentUserService.UserId, currentUserService.Role, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException exception) { return NotFound(new { error = exception.Message }); }
        catch (UnauthorizedAccessException exception) { return StatusCode(StatusCodes.Status403Forbidden, new { error = exception.Message }); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { error = exception.Message }); }
    }

    [HttpPost("{id:guid}/ec/request-information")]
    public async Task<IActionResult> EcRequestInformation(Guid id, [FromBody] EcRequestInformationInput input, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated) return Unauthorized();
        try
        {
            var workflowAction = new WorkflowActionDto
            {
                Action = "ec-request-documents",
                Reason = input.RequestedInformation ?? input.Comment,
                Comment = input.Comment ?? input.RequestedInformation
            };
            var result = await service.ExecuteActionAsync(id, workflowAction, currentUserService.UserId, currentUserService.Role, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException exception) { return NotFound(new { error = exception.Message }); }
        catch (UnauthorizedAccessException exception) { return StatusCode(StatusCodes.Status403Forbidden, new { error = exception.Message }); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { error = exception.Message }); }
    }

    [HttpPost("{id:guid}/ec/approve")]
    public async Task<IActionResult> EcApprove(Guid id, [FromBody] EcDecisionInput? input, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated) return Unauthorized();
        try
        {
            var workflowAction = new WorkflowActionDto
            {
                Action = "ec-approve",
                Comment = input?.Comment
            };
            var result = await service.ExecuteActionAsync(id, workflowAction, currentUserService.UserId, currentUserService.Role, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException exception) { return NotFound(new { error = exception.Message }); }
        catch (UnauthorizedAccessException exception) { return StatusCode(StatusCodes.Status403Forbidden, new { error = exception.Message }); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { error = exception.Message }); }
    }

    [HttpPost("{id:guid}/ec/reject")]
    public async Task<IActionResult> EcReject(Guid id, [FromBody] EcDecisionInput input, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated) return Unauthorized();
        try
        {
            var workflowAction = new WorkflowActionDto
            {
                Action = "ec-reject",
                Reason = input.Reason ?? input.Comment ?? "Rejected by Export Control",
                Comment = input.Comment ?? input.Reason
            };
            var result = await service.ExecuteActionAsync(id, workflowAction, currentUserService.UserId, currentUserService.Role, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException exception) { return NotFound(new { error = exception.Message }); }
        catch (UnauthorizedAccessException exception) { return StatusCode(StatusCodes.Status403Forbidden, new { error = exception.Message }); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { error = exception.Message }); }
    }
}

public sealed class EcRequestInformationInput
{
    public string? RequestedInformation { get; set; }
    public string? Comment { get; set; }
}

public sealed class EcDecisionInput
{
    public string? Comment { get; set; }
    public string? Reason { get; set; }
}
