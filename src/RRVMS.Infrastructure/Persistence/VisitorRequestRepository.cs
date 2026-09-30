using System.Data;
using Microsoft.EntityFrameworkCore;
using RRVMS.Application.Abstractions;
using RRVMS.Application.Common;
using RRVMS.Application.Contracts;
using RRVMS.Application.Mapping;
using RRVMS.Domain.Entities;
using RRVMS.Domain.Enums;

namespace RRVMS.Infrastructure.Persistence;

public sealed class VisitorRequestRepository(RrvmsDbContext dbContext) : IVisitorRequestRepository
{
    public Task<VisitorRequest?> GetAsync(Guid requestId, CancellationToken cancellationToken) =>
        dbContext.VisitorRequests
            .AsSplitQuery()
            .Include(request => request.Visitors).ThenInclude(visitor => visitor.Assets)
            .Include(request => request.Visitors).ThenInclude(visitor => visitor.DpsDocuments)
            .Include(request => request.Visitors).ThenInclude(visitor => visitor.ReceptionRecords)
            .Include(request => request.Visitors).ThenInclude(visitor => visitor.Versions)
            .Include(request => request.VisitDays)
            .Include(request => request.AuditEvents)
            .Include(request => request.InformationRequests)
            .Include(request => request.ScheduleChanges)
            .Include(request => request.ScreeningReviews)
            .SingleOrDefaultAsync(request => request.Id == requestId, cancellationToken);

    public Task<Visitor?> GetVisitorAsync(Guid visitorId, CancellationToken cancellationToken) =>
        dbContext.Visitors
            .AsSplitQuery()
            .Include(visitor => visitor.Assets)
            .Include(visitor => visitor.DpsDocuments)
            .Include(visitor => visitor.ReceptionRecords)
            .Include(visitor => visitor.Versions)
            .SingleOrDefaultAsync(visitor => visitor.Id == visitorId, cancellationToken);

    public async Task<IReadOnlyList<VisitorRequest>> ListAsync(RequestQuery query, CancellationToken cancellationToken) =>
        await ApplyQuery(query)
            .AsNoTracking()
            .AsSplitQuery()
            .Include(request => request.Visitors)
            .OrderByDescending(request => request.UpdatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

    public Task<int> CountAsync(RequestQuery query, CancellationToken cancellationToken) =>
        ApplyQuery(query).CountAsync(cancellationToken);

    public async Task<DashboardResponse> DashboardAsync(UserRole role, string actorId, DateOnly today, CancellationToken cancellationToken)
    {
        var visible = VisibleTo(role, actorId).AsNoTracking();
        var visitors = visible.SelectMany(request => request.Visitors);
        var todayRecords =
            from request in visible
            from visitor in request.Visitors
            from record in visitor.ReceptionRecords
            join day in dbContext.VisitDays on record.VisitDayId equals day.Id
            where day.Date == today
            select record;
        var pendingActions = role switch
        {
            UserRole.HostRequester => await visible.CountAsync(request =>
                request.Status == VisitorRequestStatus.Draft
                || request.Status == VisitorRequestStatus.VisitorDetailsPending
                || request.Status == VisitorRequestStatus.ReadyForScreening
                || request.Status == VisitorRequestStatus.PendingCorrection,
                cancellationToken),
            UserRole.ExportControl => await visible.CountAsync(request =>
                request.Status == VisitorRequestStatus.PendingScreening
                || request.Status == VisitorRequestStatus.CorrectionSubmitted,
                cancellationToken),
            UserRole.Reception => await todayRecords.CountAsync(record =>
                record.Status == ReceptionStatus.Upcoming
                || record.Status == ReceptionStatus.VerificationComplete
                || record.Status == ReceptionStatus.CheckedIn,
                cancellationToken),
            _ => 0
        };
        var recent = await visible
            .AsSplitQuery()
            .Include(request => request.Visitors)
            .OrderByDescending(request => request.UpdatedAt)
            .Take(6)
            .ToListAsync(cancellationToken);
        return new DashboardResponse(
            await visitors.CountAsync(cancellationToken),
            pendingActions,
            await todayRecords.CountAsync(cancellationToken),
            await todayRecords.CountAsync(record => record.Status == ReceptionStatus.CheckedIn, cancellationToken),
            await todayRecords.CountAsync(record => record.Status == ReceptionStatus.Upcoming || record.Status == ReceptionStatus.VerificationComplete, cancellationToken),
            await todayRecords.CountAsync(record => record.Status == ReceptionStatus.NoShow, cancellationToken),
            await visible.CountAsync(request => request.Status == VisitorRequestStatus.PendingScreening, cancellationToken),
            await visible.CountAsync(request => request.Status == VisitorRequestStatus.PendingCorrection, cancellationToken),
            await visible.CountAsync(request => request.Status == VisitorRequestStatus.Approved || request.Status == VisitorRequestStatus.PartiallyApproved, cancellationToken),
            await todayRecords.CountAsync(record => record.Status == ReceptionStatus.Completed, cancellationToken),
            recent.Select(ResponseMapper.ToSummary).ToArray());
    }

    public async Task<PagedResponse<ReportRowResponse>> ReportAsync(ReportQuery query, CancellationToken cancellationToken)
    {
        var visible = VisibleTo(query.Role, query.ActorId).AsNoTracking();
        var rows =
            from request in visible
            from visitor in request.Visitors
            from reception in visitor.ReceptionRecords
            join day in dbContext.VisitDays on reception.VisitDayId equals day.Id
            select new ReportProjection(
                visitor.Id,
                request.RequestNumber,
                visitor.FullName,
                visitor.CompanyName,
                request.SiteCode,
                request.MainHostName,
                request.HostDepartment,
                request.VisitorType,
                request.VisitStart,
                request.VisitEnd,
                request.Status,
                visitor.ScreeningDecision,
                reception.Status,
                reception.IdentityStatus,
                reception.AssetsStatus,
                request.ContractorType == ContractorType.FacilitiesContractor || visitor.Classification == VisitorClassification.Vendor
                    ? "Orange - Vendor"
                    : request.ContractorType == ContractorType.GtreContractor ? "Red - GTRE" : "Red - Visitor",
                reception.BadgeId,
                reception.VerificationRemarks,
                day.Date);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            rows = rows.Where(row => row.RequestNumber.Contains(search) || row.VisitorName.Contains(search) || row.CompanyName.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.SiteCode)) rows = rows.Where(row => row.SiteCode == query.SiteCode);
        if (query.From is not null)
        {
            var from = query.From.Value;
            rows = rows.Where(row => row.VisitDate >= from);
        }

        if (query.To is not null)
        {
            var to = query.To.Value;
            rows = rows.Where(row => row.VisitDate <= to);
        }
        if (query.Status is not null)
        {
            var status = query.Status.Value;
            rows = rows.Where(row => row.RequestStatus == status);
        }

        var total = await rows.CountAsync(cancellationToken);
        var page = await rows
            .OrderByDescending(row => row.VisitDate)
            .ThenBy(row => row.RequestNumber)
            .ThenBy(row => row.VisitorName)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
        var visitorIds = page.Select(row => row.VisitorId).Distinct().ToArray();
        var assets = await dbContext.DeclaredAssets
            .AsNoTracking()
            .Where(asset => visitorIds.Contains(asset.VisitorId))
            .OrderBy(asset => asset.AssetType)
            .ThenBy(asset => asset.SerialNumber)
            .ToListAsync(cancellationToken);
        var assetLookup = assets
            .GroupBy(asset => asset.VisitorId)
            .ToDictionary(
                group => group.Key,
                group => string.Join("; ", group.Select(asset => $"{asset.AssetType}: {asset.SerialNumber}")));
        var response = page.Select(row => new ReportRowResponse(
            row.RequestNumber,
            row.VisitorName,
            row.CompanyName,
            row.SiteCode,
            row.HostName,
            row.HostDepartment,
            row.VisitorType.ToString(),
            row.VisitStart,
            row.VisitEnd,
            row.RequestStatus,
            row.ScreeningDecision,
            row.ReceptionStatus,
            row.IdentityStatus,
            row.AssetsStatus,
            assetLookup.GetValueOrDefault(row.VisitorId, string.Empty),
            row.BadgeType,
            row.BadgeId,
            row.VerificationRemarks)).ToArray();
        return new PagedResponse<ReportRowResponse>(response, query.Page, query.PageSize, total);
    }

    public async Task<int> NextSequenceAsync(int year, int month, CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var sequence = await dbContext.RequestSequences
                .FromSqlInterpolated($"SELECT * FROM [RequestSequences] WITH (UPDLOCK, HOLDLOCK) WHERE [Year] = {year} AND [Month] = {month}")
                .SingleOrDefaultAsync(cancellationToken);
            if (sequence is null)
            {
                sequence = new RequestSequence(year, month);
                await dbContext.RequestSequences.AddAsync(sequence, cancellationToken);
            }

            var value = sequence.Next();
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return value;
        });
    }

    public Task<bool> ActiveBadgeExistsAsync(string badgeId, Guid excludingRecordId, CancellationToken cancellationToken) =>
        dbContext.ReceptionRecords.AnyAsync(
            record => record.Id != excludingRecordId
                && record.BadgeId == badgeId
                && record.Status == ReceptionStatus.CheckedIn,
            cancellationToken);

    public async Task AddAsync(VisitorRequest request, CancellationToken cancellationToken) =>
        await dbContext.VisitorRequests.AddAsync(request, cancellationToken);

    public async Task AddDocumentAccessAsync(DocumentAccessEvent accessEvent, CancellationToken cancellationToken) =>
        await dbContext.DocumentAccessEvents.AddAsync(accessEvent, cancellationToken);

    public async Task AddReportExportAsync(ReportExportEvent exportEvent, CancellationToken cancellationToken) =>
        await dbContext.ReportExportEvents.AddAsync(exportEvent, cancellationToken);

    public async Task AddOutboxAsync(OutboxMessage message, CancellationToken cancellationToken) =>
        await dbContext.OutboxMessages.AddAsync(message, cancellationToken);

    public async Task<IReadOnlyList<VisitorRequest>> ExpiredAsync(DateTimeOffset now, int take, CancellationToken cancellationToken) =>
        await dbContext.VisitorRequests
            .Include(request => request.Visitors).ThenInclude(visitor => visitor.DpsDocuments)
            .Where(request => request.RetainUntil != null && request.RetainUntil <= now)
            .OrderBy(request => request.RetainUntil)
            .Take(take)
            .ToListAsync(cancellationToken);

    public void SetExpectedRowVersion(VisitorRequest request, byte[] rowVersion) =>
        dbContext.Entry(request).Property(item => item.RowVersion).OriginalValue = rowVersion;

    public void Remove(VisitorRequest request) => dbContext.VisitorRequests.Remove(request);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException("This request changed after you opened it. Refresh and try again.") { Source = exception.Source };
        }
        catch (DbUpdateException exception) when (exception.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true)
        {
            throw new ConflictException("The operation conflicts with an existing record.") { Source = exception.Source };
        }
    }

    private IQueryable<VisitorRequest> ApplyQuery(RequestQuery query)
    {
        var requests = dbContext.VisitorRequests.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            requests = requests.Where(request =>
                request.RequestNumber.Contains(search)
                || request.MainHostName.Contains(search)
                || request.Visitors.Any(visitor => visitor.FullName.Contains(search) || visitor.CompanyName.Contains(search)));
        }

        if (query.Status is not null)
        {
            var status = query.Status.Value;
            requests = requests.Where(request => request.Status == status);
        }

        if (query.AllowedStatuses is { Count: > 0 })
        {
            requests = requests.Where(request => query.AllowedStatuses.Contains(request.Status));
        }

        if (!string.IsNullOrWhiteSpace(query.SiteCode))
        {
            requests = requests.Where(request => request.SiteCode == query.SiteCode);
        }

        if (query.VisitDate is not null)
        {
            var visitDate = query.VisitDate.Value;
            requests = requests.Where(request => request.VisitDays.Any(day => day.Date == visitDate));
        }

        if (!string.IsNullOrWhiteSpace(query.RequesterObjectId))
        {
            requests = requests.Where(request => request.RequesterObjectId == query.RequesterObjectId);
        }

        return requests;
    }

    private IQueryable<VisitorRequest> VisibleTo(UserRole role, string actorId)
    {
        var requests = dbContext.VisitorRequests.AsQueryable();
        return role switch
        {
            UserRole.HostRequester => requests.Where(request => request.RequesterObjectId == actorId),
            UserRole.ExportControl => requests.Where(request => request.VisitorType == VisitorType.External),
            UserRole.Reception => requests.Where(request =>
                request.Status == VisitorRequestStatus.Approved
                || request.Status == VisitorRequestStatus.PartiallyApproved
                || request.Status == VisitorRequestStatus.VisitCompleted),
            _ => requests.Where(_ => false)
        };
    }

    private sealed record ReportProjection(
        Guid VisitorId,
        string RequestNumber,
        string VisitorName,
        string CompanyName,
        string SiteCode,
        string HostName,
        string HostDepartment,
        VisitorType VisitorType,
        DateTimeOffset VisitStart,
        DateTimeOffset VisitEnd,
        VisitorRequestStatus RequestStatus,
        ScreeningDecision ScreeningDecision,
        ReceptionStatus ReceptionStatus,
        VerificationStatus IdentityStatus,
        VerificationStatus AssetsStatus,
        string BadgeType,
        string BadgeId,
        string VerificationRemarks,
        DateOnly VisitDate);
}
