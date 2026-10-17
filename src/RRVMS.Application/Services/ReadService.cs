using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using RRVMS.Application.Abstractions;
using RRVMS.Application.Common;
using RRVMS.Application.Contracts;
using RRVMS.Domain.Enums;
using RRVMS.Domain.Entities;

namespace RRVMS.Application.Services;

public sealed class ReadService(
    IVisitorRequestRepository repository,
    ICurrentActor actor,
    IClock clock,
    TimeZoneInfo businessTimeZone)
{
    public Task<DashboardResponse> DashboardAsync(CancellationToken cancellationToken)
    {
        EnsureAuthenticated();
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.UtcNow, businessTimeZone).Date);
        return repository.DashboardAsync(actor.Role, actor.ObjectId, today, cancellationToken);
    }

    public Task<ReportPageResponse> ReportAsync(
        string? search,
        string? siteCode,
        DateOnly? from,
        DateOnly? to,
        VisitorRequestStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken,
        ScreeningDecision? screening = null,
        ReceptionStatus? reception = null,
        VerificationStatus? identity = null,
        string? assets = null,
        string? personType = null,
        bool history = false)
    {
        EnsureAuthenticated();
        ValidateFilters(search, siteCode, from, to, assets, personType);
        page = Math.Clamp(page, 1, 1000000);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.UtcNow, businessTimeZone).Date);
        return repository.ReportAsync(new ReportQuery(search, siteCode, from, to, status, actor.Role, actor.ObjectId, page, pageSize, screening, reception, identity, assets, personType, history, today), cancellationToken);
    }

    public async Task<byte[]> ExportCsvAsync(
        string? search,
        string? siteCode,
        DateOnly? from,
        DateOnly? to,
        VisitorRequestStatus? status,
        CancellationToken cancellationToken,
        ScreeningDecision? screening = null,
        ReceptionStatus? reception = null,
        VerificationStatus? identity = null,
        string? assets = null,
        string? personType = null,
        bool history = false)
    {
        EnsureAuthenticated();
        ValidateFilters(search, siteCode, from, to, assets, personType);
        var result = await repository.ReportAsync(new ReportQuery(search, siteCode, from, to, status, actor.Role, actor.ObjectId, 1, 10000, screening, reception, identity, assets, personType, history, DailyRows: true), cancellationToken);
        EnsureExportWithinLimit(result.Total);
        var csv = new StringBuilder();
        csv.AppendLine("Request Number,Visitor,Company,Site,Host,Department,Person Type,Visit Date,Visit Start,Visit End,Request Status,Screening,Reception,Identity,Assets,Asset Summary,Badge Type,Badge ID,Remarks");
        foreach (var row in result.Items)
        {
            var fields = new[]
            {
                row.RequestNumber, row.VisitorName, row.CompanyName, row.SiteCode, row.HostName, row.HostDepartment,
                row.PersonType, row.VisitDate.ToString("yyyy-MM-dd"), row.VisitStart.ToString("O"), row.VisitEnd.ToString("O"), row.RequestStatus.ToString(),
                row.ScreeningDecision.ToString(), row.ReceptionStatus.ToString(), row.IdentityStatus.ToString(),
                row.AssetsStatus.ToString(), row.AssetSummary, row.BadgeType, row.BadgeId, row.VerificationRemarks
            };
            csv.AppendLine(string.Join(',', fields.Select(EscapeCsv)));
        }

        var content = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        await RecordExportAsync("CSV", search, siteCode, from, to, status, result.Total, cancellationToken, screening, reception, identity, assets, personType, history);
        return content;
    }

    public async Task<byte[]> ExportExcelAsync(
        string? search,
        string? siteCode,
        DateOnly? from,
        DateOnly? to,
        VisitorRequestStatus? status,
        CancellationToken cancellationToken,
        ScreeningDecision? screening = null,
        ReceptionStatus? reception = null,
        VerificationStatus? identity = null,
        string? assets = null,
        string? personType = null,
        bool history = false)
    {
        EnsureAuthenticated();
        ValidateFilters(search, siteCode, from, to, assets, personType);
        var result = await repository.ReportAsync(new ReportQuery(search, siteCode, from, to, status, actor.Role, actor.ObjectId, 1, 10000, screening, reception, identity, assets, personType, history, DailyRows: true), cancellationToken);
        EnsureExportWithinLimit(result.Total);
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Visitor report");
        var headers = new[]
        {
            "Request Number", "Visitor", "Company", "Site", "Host", "Department", "Person Type", "Visit Date", "Visit Start", "Visit End",
            "Request Status", "Screening", "Reception", "Identity", "Assets", "Asset Summary", "Badge Type", "Badge ID", "Remarks"
        };
        for (var column = 0; column < headers.Length; column++)
        {
            sheet.Cell(1, column + 1).Value = headers[column];
        }

        for (var index = 0; index < result.Items.Count; index++)
        {
            var item = result.Items[index];
            var values = new[]
            {
                item.RequestNumber, item.VisitorName, item.CompanyName, item.SiteCode, item.HostName, item.HostDepartment, item.PersonType,
                item.VisitDate.ToString("yyyy-MM-dd"), item.VisitStart.ToString("O"), item.VisitEnd.ToString("O"), item.RequestStatus.ToString(), item.ScreeningDecision.ToString(),
                item.ReceptionStatus.ToString(), item.IdentityStatus.ToString(), item.AssetsStatus.ToString(), item.AssetSummary,
                item.BadgeType, item.BadgeId, item.VerificationRemarks
            };
            for (var column = 0; column < values.Length; column++)
            {
                sheet.Cell(index + 2, column + 1).Value = SafeSpreadsheetValue(values[column]);
            }
        }

        var range = sheet.Range(1, 1, Math.Max(result.Items.Count + 1, 2), headers.Length);
        range.CreateTable("VisitorReport");
        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents();
        foreach (var column in sheet.ColumnsUsed().Where(column => column.Width > 60))
        {
            column.Width = 60;
        }
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();
        await RecordExportAsync("XLSX", search, siteCode, from, to, status, result.Total, cancellationToken, screening, reception, identity, assets, personType, history);
        return content;
    }

    private static string EscapeCsv(string value)
    {
        var safe = SafeSpreadsheetValue(value);

        return "\"" + safe.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private static string SafeSpreadsheetValue(string value) =>
        value.Length > 0 && value[0] is '=' or '+' or '-' or '@' or '\t' or '\r' ? "'" + value : value;

    private static void EnsureExportWithinLimit(int total)
    {
        if (total > 10000)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["filters"] = ["The export contains more than 10,000 rows. Narrow the date range or filters and try again."]
            });
        }
    }

    private void EnsureAuthenticated()
    {
        if (!actor.IsAuthenticated)
        {
            throw new ForbiddenException("Authentication is required.");
        }
    }

    private static void ValidateFilters(string? search, string? siteCode, DateOnly? from, DateOnly? to, string? assets, string? personType)
    {
        var errors = new Dictionary<string, string[]>();
        if (search?.Length > 200) errors["search"] = ["Search cannot exceed 200 characters."];
        if (siteCode?.Length > 50) errors["siteCode"] = ["Site cannot exceed 50 characters."];
        if (from is not null && to is not null && from > to) errors["dateRange"] = ["The From date must not be after the To date."];
        if (!string.IsNullOrWhiteSpace(assets) && assets is not ("DECLARED" or "NONE" or "VERIFIED" or "REJECTED")) errors["assets"] = ["Select a valid assets filter."];
        if (personType?.Length > 50) errors["personType"] = ["Person type cannot exceed 50 characters."];
        if (errors.Count > 0) throw new ValidationException(errors);
    }

    private async Task RecordExportAsync(
        string format,
        string? search,
        string? siteCode,
        DateOnly? from,
        DateOnly? to,
        VisitorRequestStatus? status,
        int rowCount,
        CancellationToken cancellationToken,
        ScreeningDecision? screening,
        ReceptionStatus? reception,
        VerificationStatus? identity,
        string? assets,
        string? personType,
        bool history)
    {
        var filters = JsonSerializer.Serialize(new { search, siteCode, from, to, Status = status?.ToString(), screening, reception, identity, assets, personType, history });
        await repository.AddReportExportAsync(new ReportExportEvent(actor.ObjectId, actor.Role.ToString(), format, filters, rowCount, actor.CorrelationId, clock.UtcNow), cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }
}
