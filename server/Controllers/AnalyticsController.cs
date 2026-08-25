using System.Text;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RRVMS.Api.Data;
using RRVMS.Api.Services;

namespace RRVMS.Api.Controllers;

[ApiController]
[Route("api/analytics")]
public sealed class AnalyticsController(RrvmsDbContext db, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.Role != "EXPORT_CONTROL") return Forbid();
        var rows = await ReadRows(cancellationToken);
        return Ok(new { totalRequests = rows.Count, byStatus = rows.GroupBy(row => row.Status).ToDictionary(group => group.Key, group => group.Count()), rows });
    }

    [HttpGet("export.csv")]
    public async Task<IActionResult> Csv(CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.Role != "EXPORT_CONTROL") return Forbid();
        var rows = await ReadRows(cancellationToken); var csv = new StringBuilder("Request Number,Visitor,Company,Visit Date,Status,Created At\n");
        foreach (var row in rows) csv.AppendLine(string.Join(',', new[] { row.RequestNumber, row.Visitor, row.Company, row.VisitDate, row.Status, row.CreatedAt }.Select(Escape)));
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "rrvms-analytics.csv");
    }

    [HttpGet("export.xlsx")]
    public async Task<IActionResult> Xlsx(CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.Role != "EXPORT_CONTROL") return Forbid();
        var rows = await ReadRows(cancellationToken); using var workbook = new XLWorkbook(); var sheet = workbook.Worksheets.Add("Analytics");
        var headers = new[] { "Request Number", "Visitor", "Company", "Visit Date", "Status", "Created At" };
        for (var index = 0; index < headers.Length; index++) sheet.Cell(1, index + 1).Value = headers[index];
        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++) { var row = rows[rowIndex]; var values = new[] { row.RequestNumber, row.Visitor, row.Company, row.VisitDate, row.Status, row.CreatedAt }; for (var columnIndex = 0; columnIndex < values.Length; columnIndex++) sheet.Cell(rowIndex + 2, columnIndex + 1).Value = values[columnIndex]; }
        sheet.Columns().AdjustToContents(); using var output = new MemoryStream(); workbook.SaveAs(output); return File(output.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "rrvms-analytics.xlsx");
    }

    private async Task<List<AnalyticsRow>> ReadRows(CancellationToken cancellationToken)
    {
        var source = await db.VisitorRequests.AsNoTracking().Include(request => request.Visitor).Include(request => request.VisitDays).SelectMany(request => request.VisitDays.DefaultIfEmpty(), (request, day) => new { request.RequestNumber, Visitor = request.Visitor.FullName, Company = request.VisitingCompany, VisitDate = day == null ? (DateOnly?)null : day.VisitDate, Status = request.Status.ToString(), request.CreatedAt }).ToListAsync(cancellationToken);
        return source.Select(row => new AnalyticsRow(row.RequestNumber, row.Visitor, row.Company, row.VisitDate?.ToString("yyyy-MM-dd") ?? string.Empty, row.Status, row.CreatedAt.ToString("O"))).ToList();
    }
    private static string Escape(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
    private sealed record AnalyticsRow(string RequestNumber, string Visitor, string Company, string VisitDate, string Status, string CreatedAt);
}
