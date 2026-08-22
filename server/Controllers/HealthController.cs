using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using RRVMS.Api.Data;

namespace RRVMS.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    private readonly RrvmsDbContext dbContext;
    private readonly ILogger<HealthController> logger;

    public HealthController(RrvmsDbContext dbContext, ILogger<HealthController> logger)
    {
        this.dbContext = dbContext;
        this.logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.Database.SqlQueryRaw<int>("SELECT 1 AS \"Value\"").SingleAsync(cancellationToken);
            return Ok(new { status = "ok", service = "RRVMS API", database = "connected" });
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Database health check failed");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "unhealthy",
                service = "RRVMS API",
                database = "unavailable"
            });
        }
    }
}
