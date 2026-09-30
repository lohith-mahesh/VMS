using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RRVMS.Application.Abstractions;
using RRVMS.Application.Contracts;
using RRVMS.Application.Services;
using RRVMS.Domain.Enums;
using RRVMS.Infrastructure.Persistence;

namespace RRVMS.Api.Endpoints;

public static class AppEndpoints
{
    public static IEndpointRouteBuilder MapAppEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" })).AllowAnonymous().ExcludeFromDescription();
        endpoints.MapGet("/health/ready", async (RrvmsDbContext dbContext, CancellationToken cancellationToken) =>
            await dbContext.Database.CanConnectAsync(cancellationToken)
                ? Results.Ok(new { status = "Healthy" })
                : Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Database unavailable"))
            .AllowAnonymous()
            .ExcludeFromDescription();
        endpoints.MapGet("/api/me", (ICurrentActor actor) => Results.Ok(new MeResponse(actor.ObjectId, actor.DisplayName, actor.Email, actor.Role)))
            .RequireAuthorization()
            .WithTags("Identity");
        endpoints.MapGet("/api/directory", async ([FromQuery] string query, IEmployeeDirectory directory, CancellationToken cancellationToken) =>
            Results.Ok(await directory.SearchAsync(query, cancellationToken)))
            .RequireAuthorization("HostRequester")
            .WithTags("Directory");
        endpoints.MapGet("/api/dashboard", async (ReadService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.DashboardAsync(cancellationToken)))
            .RequireAuthorization()
            .WithTags("Dashboard");
        endpoints.MapGet("/api/reports", ReportAsync)
            .RequireAuthorization()
            .WithTags("Reports");
        endpoints.MapGet("/api/reports/export", ExportAsync)
            .RequireAuthorization()
            .WithTags("Reports");
        endpoints.MapGet("/api/reports/export.xlsx", ExportExcelAsync)
            .RequireAuthorization()
            .WithTags("Reports");
        return endpoints;
    }

    private static async Task<IResult> ReportAsync(
        ReadService service,
        [FromQuery] string? search,
        [FromQuery] string? siteCode,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] VisitorRequestStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        Results.Ok(await service.ReportAsync(search, siteCode, from, to, status, page, pageSize, cancellationToken));

    private static async Task<IResult> ExportAsync(
        ReadService service,
        [FromQuery] string? search,
        [FromQuery] string? siteCode,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] VisitorRequestStatus? status,
        CancellationToken cancellationToken)
    {
        var content = await service.ExportCsvAsync(search, siteCode, from, to, status, cancellationToken);
        return Results.File(content, "text/csv; charset=utf-8", $"rrvms-report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
    }

    private static async Task<IResult> ExportExcelAsync(
        ReadService service,
        [FromQuery] string? search,
        [FromQuery] string? siteCode,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] VisitorRequestStatus? status,
        CancellationToken cancellationToken)
    {
        var content = await service.ExportExcelAsync(search, siteCode, from, to, status, cancellationToken);
        return Results.File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"rrvms-report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx");
    }
}
