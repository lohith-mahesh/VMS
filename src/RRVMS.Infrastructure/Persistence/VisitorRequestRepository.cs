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
            where day.Date == today && (role != UserRole.Reception || visitor.ScreeningDecision == ScreeningDecision.Approved)
            select record;
        var allRecords = visitors.SelectMany(visitor => visitor.ReceptionRecords);
        var pendingReview = visible.Where(request =>
            request.Status == VisitorRequestStatus.PendingEcReview
            || request.Status == VisitorRequestStatus.DocumentationSubmitted
            || request.Status == VisitorRequestStatus.EcReReviewRequired);
        var pendingDocs = visible.Where(request => request.Status == VisitorRequestStatus.PendingDocumentation);
        var pendingActions = role switch
        {
            UserRole.HostRequester => await visible.CountAsync(request =>
                request.Status == VisitorRequestStatus.Draft
                || request.Status == VisitorRequestStatus.VisitorFormPending
                || request.Status == VisitorRequestStatus.VisitorFormSubmitted
                || request.Status == VisitorRequestStatus.HostReview
                || request.Status == VisitorRequestStatus.PendingDocumentation
                || request.Status == VisitorRequestStatus.PendingEcReview
                || request.Status == VisitorRequestStatus.DocumentationSubmitted
                || request.Status == VisitorRequestStatus.EcReReviewRequired,
                cancellationToken),
            UserRole.ExportControl => await visible.CountAsync(request =>
                request.Status == VisitorRequestStatus.PendingEcReview
                || request.Status == VisitorRequestStatus.DocumentationSubmitted
                || request.Status == VisitorRequestStatus.EcReReviewRequired,
                cancellationToken),
            UserRole.Reception => await todayRecords.CountAsync(record =>
                record.Status == ReceptionStatus.Upcoming
                || record.Status == ReceptionStatus.ReceptionVerification
                || record.Status == ReceptionStatus.CheckedIn,
                cancellationToken),
            _ => 0
        };
        var recent = await visible
            .AsSplitQuery()
            .Include(request => request.Visitors)
            .OrderByDescending(request => request.CreatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);
        var reviewItems = role == UserRole.ExportControl
            ? await pendingReview.AsSplitQuery().Include(request => request.Visitors)
                .OrderByDescending(request => request.CreatedAt).Take(100).ToListAsync(cancellationToken)
            : new List<VisitorRequest>();
        var documentItems = role == UserRole.ExportControl
            ? await pendingDocs.AsSplitQuery().Include(request => request.Visitors)
                .OrderByDescending(request => request.CreatedAt).Take(100).ToListAsync(cancellationToken)
            : new List<VisitorRequest>();
        var receptionItems = new List<ReceptionIndexRowResponse>();
        if (role == UserRole.Reception)
        {
            var receptionRequests = await visible.AsSplitQuery()
                .Include(request => request.VisitDays)
                .Include(request => request.Visitors).ThenInclude(visitor => visitor.ReceptionRecords)
                .Include(request => request.Visitors).ThenInclude(visitor => visitor.Assets)
                .Where(request => request.Visitors.Any(visitor => visitor.ScreeningDecision == ScreeningDecision.Approved
                    && visitor.ReceptionRecords.Any(record => record.Status == ReceptionStatus.CheckedIn
                        || dbContext.VisitDays.Any(day => day.Id == record.VisitDayId && day.Date == today))))
                .ToListAsync(cancellationToken);
            foreach (var request in receptionRequests)
            {
                var todayDay = request.VisitDays.FirstOrDefault(day => day.Date == today);
                foreach (var visitor in request.Visitors.Where(visitor => visitor.ScreeningDecision == ScreeningDecision.Approved))
                {
                    var selected = visitor.ReceptionRecords.FirstOrDefault(record => record.Status == ReceptionStatus.CheckedIn)
                        ?? visitor.ReceptionRecords.FirstOrDefault(record => record.VisitDayId == todayDay?.Id);
                    if (selected is null) continue;
                    var day = request.VisitDays.First(day => day.Id == selected.VisitDayId);
                    var personType = visitor.Classification == VisitorClassification.Vendor ? "Vendor"
                        : request.ContractorType == ContractorType.GtreContractor ? "GTRE"
                        : request.VisitorType == VisitorType.Internal ? "Internal Visitor" : "External Visitor";
                    receptionItems.Add(new ReceptionIndexRowResponse(request.Id, visitor.Id, day.Id,
                        request.RequestNumber, visitor.FullName, visitor.CompanyName, request.MainHostName,
                        request.EscortingHostName, personType, day.Date, todayDay is not null,
                        selected.Status, visitor.ScreeningDecision, selected.IdentityStatus, selected.AssetsStatus,
                        selected.BadgeId, selected.BadgeType, string.Join(", ", visitor.Assets.Select(asset => asset.AssetType))));
                }
            }
        }
        return new DashboardResponse(
            await visitors.CountAsync(cancellationToken),
            pendingActions,
            role == UserRole.Reception ? receptionItems.Count(item => item.ScheduledToday)
                : await todayRecords.Select(record => record.VisitorId).Distinct().CountAsync(cancellationToken),
            role == UserRole.Reception ? receptionItems.Count(item => item.Status == ReceptionStatus.CheckedIn)
                : await allRecords.Where(record => record.Status == ReceptionStatus.CheckedIn).Select(record => record.VisitorId).Distinct().CountAsync(cancellationToken),
            role == UserRole.Reception
                ? receptionItems.Count(item => item.Status == ReceptionStatus.Upcoming
                    || item.Status == ReceptionStatus.VerificationInProgress
                    || item.Status == ReceptionStatus.ReceptionVerification)
                : await (from request in visible from visitor in request.Visitors from record in visitor.ReceptionRecords
                   join day in dbContext.VisitDays on record.VisitDayId equals day.Id
                   where day.Date > today && record.Status == ReceptionStatus.Upcoming
                   select visitor.Id).Distinct().CountAsync(cancellationToken),
            role == UserRole.Reception
                ? receptionItems.Count(item => item.Status == ReceptionStatus.NoShow)
                : await allRecords.Where(record => record.Status == ReceptionStatus.NoShow).Select(record => record.VisitorId).Distinct().CountAsync(cancellationToken),
            await visible.CountAsync(request => request.Status == VisitorRequestStatus.PendingEcReview || request.Status == VisitorRequestStatus.DocumentationSubmitted || request.Status == VisitorRequestStatus.EcReReviewRequired, cancellationToken),
            await visible.CountAsync(request => request.Status == VisitorRequestStatus.PendingDocumentation, cancellationToken),
            await visible.CountAsync(request => request.Status == VisitorRequestStatus.Approved || request.Status == VisitorRequestStatus.PartiallyApproved, cancellationToken),
            role == UserRole.Reception
                ? receptionItems.Count(item => item.Status == ReceptionStatus.Completed)
                : await allRecords.Where(record => record.Status == ReceptionStatus.Completed).Select(record => record.VisitorId).Distinct().CountAsync(cancellationToken),
            recent.Select(ResponseMapper.ToSummary).ToArray(),
            await visible.CountAsync(request => request.Status == VisitorRequestStatus.Rejected, cancellationToken),
            role == UserRole.Reception
                ? receptionItems.Count(item => item.Status == ReceptionStatus.EntryRejected)
                : await visible.CountAsync(request => request.Visitors.Any(visitor => visitor.ReceptionRecords.Any(record => record.Status == ReceptionStatus.EntryRejected)), cancellationToken),
            await visible.CountAsync(request => request.Status == VisitorRequestStatus.Approved
                || request.Status == VisitorRequestStatus.PartiallyApproved
                || request.Status == VisitorRequestStatus.Rejected
                || request.Status == VisitorRequestStatus.Cancelled
                || request.Status == VisitorRequestStatus.VisitProcessCompleted, cancellationToken),
            reviewItems.Select(ResponseMapper.ToSummary).ToArray(),
            documentItems.Select(ResponseMapper.ToSummary).ToArray(),
            receptionItems.OrderBy(item => item.VisitorName).ToArray());
    }

    public async Task<ReportPageResponse> ReportAsync(ReportQuery query, CancellationToken cancellationToken)
    {
        var visible = (query.Role == UserRole.Reception
            ? dbContext.VisitorRequests.AsQueryable()
            : VisibleTo(query.Role, query.ActorId)).AsNoTracking();
        var rows =
            from request in visible
            from visitor in request.Visitors
            from reception in visitor.ReceptionRecords
            join day in dbContext.VisitDays on reception.VisitDayId equals day.Id
            select new ReportProjection(
                request.Id,
                visitor.Id,
                visitor.Assets.Count,
                request.RequestNumber,
                visitor.FullName,
                visitor.CompanyName,
                request.SiteCode,
                request.MainHostName,
                request.HostDepartment,
                visitor.Classification == VisitorClassification.Vendor ? "Vendor"
                    : request.ContractorType == ContractorType.GtreContractor ? "GTRE"
                    : request.VisitorType == VisitorType.Internal ? "Internal Visitor" : "External Visitor",
                request.VisitStart,
                request.VisitEnd,
                request.Status,
                visitor.ScreeningDecision,
                reception.Status,
                reception.IdentityStatus,
                reception.AssetsStatus,
                visitor.Classification == VisitorClassification.Vendor
                    ? "Orange-Vendors"
                    : request.ContractorType == ContractorType.GtreContractor ? "Red-GTRE" : "Red-Visitor",
                reception.BadgeId,
                reception.VerificationRemarks,
                day.Date);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            rows = rows.Where(row => row.RequestNumber.Contains(search) || row.VisitorName.Contains(search)
                || row.CompanyName.Contains(search) || row.HostName.Contains(search)
                || row.HostDepartment.Contains(search) || row.BadgeId.Contains(search)
                || dbContext.DeclaredAssets.Any(asset => asset.VisitorId == row.VisitorId
                    && (asset.AssetType.Contains(search)
                        || (query.Role != UserRole.Reception && asset.SerialNumber.Contains(search)))));
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
        if (query.Screening is not null) rows = rows.Where(row => row.ScreeningDecision == query.Screening);
        if (query.Reception is not null) rows = rows.Where(row => row.ReceptionStatus == query.Reception);
        if (query.Identity is not null) rows = rows.Where(row => row.IdentityStatus == query.Identity);
        if (!string.IsNullOrWhiteSpace(query.PersonType)) rows = rows.Where(row => row.PersonType == query.PersonType);
        if (query.History) rows = rows.Where(row => row.RequestStatus == VisitorRequestStatus.Approved
            || row.RequestStatus == VisitorRequestStatus.PartiallyApproved
            || row.RequestStatus == VisitorRequestStatus.Rejected
            || row.RequestStatus == VisitorRequestStatus.Cancelled
            || row.RequestStatus == VisitorRequestStatus.VisitProcessCompleted);
        rows = query.Assets switch
        {
            "DECLARED" => rows.Where(row => row.AssetCount > 0),
            "NONE" => rows.Where(row => row.AssetCount == 0),
            "VERIFIED" => rows.Where(row => row.AssetCount > 0 && row.AssetsStatus == VerificationStatus.Approved),
            "REJECTED" => rows.Where(row => row.AssetsStatus == VerificationStatus.Rejected),
            _ => rows
        };

        var matchingVisitors = await rows.Select(row => row.VisitorId).Distinct().CountAsync(cancellationToken);
        var matchingRequests = await rows.Select(row => row.RequestId).Distinct().CountAsync(cancellationToken);
        var checkedIn = await rows.Where(row => row.ReceptionStatus == ReceptionStatus.CheckedIn)
            .Select(row => row.VisitorId).Distinct().CountAsync(cancellationToken);
        var assetsDeclared = await rows.Where(row => row.AssetCount > 0)
            .Select(row => row.VisitorId).Distinct().CountAsync(cancellationToken);
        int total;
        List<ReportProjection> page;
        if (query.DailyRows)
        {
            total = await rows.CountAsync(cancellationToken);
            page = await rows.OrderByDescending(row => row.VisitDate)
                .ThenBy(row => row.RequestNumber).ThenBy(row => row.VisitorName)
                .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
                .ToListAsync(cancellationToken);
        }
        else
        {
            total = matchingVisitors;
            var visitorPage = await rows.GroupBy(row => row.VisitorId)
                .Select(group => new { VisitorId = group.Key, LatestDate = group.Max(row => row.VisitDate) })
                .OrderByDescending(item => item.LatestDate).ThenBy(item => item.VisitorId)
                .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
                .ToListAsync(cancellationToken);
            var ids = visitorPage.Select(item => item.VisitorId).ToArray();
            var candidates = await rows.Where(row => ids.Contains(row.VisitorId)).ToListAsync(cancellationToken);
            var byVisitor = candidates.GroupBy(row => row.VisitorId).ToDictionary(group => group.Key,
                group => group.OrderByDescending(row => row.ReceptionStatus == ReceptionStatus.CheckedIn ? 500
                        : row.VisitDate == query.Today ? 400
                        : row.ReceptionStatus is ReceptionStatus.Completed or ReceptionStatus.NoShow or ReceptionStatus.EntryRejected ? 300
                        : row.ReceptionStatus == ReceptionStatus.ReceptionVerification ? 200 : 100)
                    .ThenByDescending(row => row.VisitDate).First());
            page = visitorPage.Select(item => byVisitor[item.VisitorId]).ToList();
        }
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
                group => string.Join("; ", group.Select(asset => query.Role == UserRole.Reception
                    ? asset.AssetType : $"{asset.AssetType}: {asset.SerialNumber}")));
        var response = page.Select(row => new ReportRowResponse(
            row.RequestId,
            row.VisitorId,
            row.VisitDate,
            row.AssetCount,
            row.RequestNumber,
            row.VisitorName,
            row.CompanyName,
            row.SiteCode,
            row.HostName,
            row.HostDepartment,
            row.PersonType,
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
        return new ReportPageResponse(response, query.Page, query.PageSize, total,
            matchingVisitors, matchingRequests, checkedIn, assetsDeclared);
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

    public Task<string?> FindDuplicateAsync(string requesterId, VisitorType visitorType, string siteCode, int visitorCount, VisitPurposeType purposeType,
        string purpose, string hostName, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken)
    {
        var normalizedPurpose = purpose.ToLower();
        var normalizedHost = hostName.ToLower();
        return dbContext.VisitorRequests.AsNoTracking()
            .Where(request => request.RequesterObjectId == requesterId && request.VisitorType == visitorType && request.SiteCode == siteCode
                && request.Visitors.Count == visitorCount && request.PurposeType == purposeType && request.Purpose.ToLower() == normalizedPurpose
                && request.MainHostName.ToLower() == normalizedHost && request.VisitStart == start && request.VisitEnd == end
                && request.Status != VisitorRequestStatus.Cancelled && request.Status != VisitorRequestStatus.Rejected
                && request.Status != VisitorRequestStatus.VisitProcessCompleted)
            .Select(request => request.RequestNumber)
            .FirstOrDefaultAsync(cancellationToken);
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

        var today = query.Today;
        requests = query.View switch
        {
            "history" => requests.Where(request => request.Status == VisitorRequestStatus.Approved
                || request.Status == VisitorRequestStatus.PartiallyApproved
                || request.Status == VisitorRequestStatus.Rejected
                || request.Status == VisitorRequestStatus.Cancelled
                || request.Status == VisitorRequestStatus.VisitProcessCompleted),
            "pending" => requests.Where(request => request.Status == VisitorRequestStatus.VisitorFormPending
                || request.Status == VisitorRequestStatus.VisitorFormSubmitted
                || request.Status == VisitorRequestStatus.HostReview
                || request.Status == VisitorRequestStatus.PendingEcReview
                || request.Status == VisitorRequestStatus.PendingDocumentation
                || request.Status == VisitorRequestStatus.DocumentationSubmitted
                || request.Status == VisitorRequestStatus.EcReReviewRequired),
            "pending-ec" => requests.Where(request => request.Status == VisitorRequestStatus.PendingEcReview
                || request.Status == VisitorRequestStatus.DocumentationSubmitted
                || request.Status == VisitorRequestStatus.EcReReviewRequired),
            "pending-documentation" => requests.Where(request => request.Status == VisitorRequestStatus.PendingDocumentation),
            "approved" => requests.Where(request => request.Status == VisitorRequestStatus.Approved || request.Status == VisitorRequestStatus.PartiallyApproved),
            "rejected" => requests.Where(request => request.Status == VisitorRequestStatus.Rejected),
            "today" => requests.Where(request => request.VisitDays.Any(day => day.Date == today)),
            "checked-in" => requests.Where(request => request.Visitors.Any(visitor => visitor.ReceptionRecords.Any(record => record.Status == ReceptionStatus.CheckedIn))),
            "checked-out" => requests.Where(request => request.Visitors.Any(visitor => visitor.ReceptionRecords.Any(record => record.Status == ReceptionStatus.Completed))),
            "no-show" => requests.Where(request => request.Visitors.Any(visitor => visitor.ReceptionRecords.Any(record => record.Status == ReceptionStatus.NoShow))),
            "reception-rejected" => requests.Where(request => request.Visitors.Any(visitor => visitor.ReceptionRecords.Any(record => record.Status == ReceptionStatus.EntryRejected))),
            "upcoming" => requests.Where(request => request.VisitDays.Any(day => day.Date > today)
                && request.Status != VisitorRequestStatus.Cancelled
                && request.Status != VisitorRequestStatus.Rejected
                && request.Status != VisitorRequestStatus.VisitProcessCompleted),
            _ => requests
        };

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
            UserRole.ExportControl => requests,
            UserRole.Reception => requests.Where(request =>
                request.Status == VisitorRequestStatus.Approved
                || request.Status == VisitorRequestStatus.PartiallyApproved
                || request.Status == VisitorRequestStatus.VisitProcessCompleted),
            _ => requests.Where(_ => false)
        };
    }

    private sealed record ReportProjection(
        Guid RequestId,
        Guid VisitorId,
        int AssetCount,
        string RequestNumber,
        string VisitorName,
        string CompanyName,
        string SiteCode,
        string HostName,
        string HostDepartment,
        string PersonType,
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
