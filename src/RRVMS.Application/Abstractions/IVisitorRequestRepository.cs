using RRVMS.Domain.Entities;
using RRVMS.Domain.Enums;
using RRVMS.Application.Contracts;

namespace RRVMS.Application.Abstractions;

public sealed record RequestQuery(
    string? Search,
    VisitorRequestStatus? Status,
    string? SiteCode,
    DateOnly? VisitDate,
    string? RequesterObjectId,
    int Page,
    int PageSize,
    IReadOnlyCollection<VisitorRequestStatus>? AllowedStatuses = null,
    string? View = null,
    DateOnly? Today = null);

public sealed record ReportQuery(
    string? Search,
    string? SiteCode,
    DateOnly? From,
    DateOnly? To,
    VisitorRequestStatus? Status,
    UserRole Role,
    string ActorId,
    int Page,
    int PageSize,
    ScreeningDecision? Screening = null,
    ReceptionStatus? Reception = null,
    VerificationStatus? Identity = null,
    string? Assets = null,
    string? PersonType = null,
    bool History = false,
    DateOnly? Today = null,
    bool DailyRows = false);

public interface IVisitorRequestRepository
{
    Task<VisitorRequest?> GetAsync(Guid requestId, CancellationToken cancellationToken);
    Task<Visitor?> GetVisitorAsync(Guid visitorId, CancellationToken cancellationToken);
    Task<IReadOnlyList<VisitorRequest>> ListAsync(RequestQuery query, CancellationToken cancellationToken);
    Task<int> CountAsync(RequestQuery query, CancellationToken cancellationToken);
    Task<DashboardResponse> DashboardAsync(UserRole role, string actorId, DateOnly today, CancellationToken cancellationToken);
    Task<ReportPageResponse> ReportAsync(ReportQuery query, CancellationToken cancellationToken);
    Task<int> NextSequenceAsync(int year, int month, CancellationToken cancellationToken);
    Task<string?> FindDuplicateAsync(string requesterId, VisitorType visitorType, string siteCode, int visitorCount, VisitPurposeType purposeType, string purpose, string hostName, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken);
    Task<bool> ActiveBadgeExistsAsync(string badgeId, Guid excludingRecordId, CancellationToken cancellationToken);
    Task AddAsync(VisitorRequest request, CancellationToken cancellationToken);
    Task AddDocumentAccessAsync(DocumentAccessEvent accessEvent, CancellationToken cancellationToken);
    Task AddReportExportAsync(ReportExportEvent exportEvent, CancellationToken cancellationToken);
    Task AddOutboxAsync(OutboxMessage message, CancellationToken cancellationToken);
    Task<IReadOnlyList<VisitorRequest>> ExpiredAsync(DateTimeOffset now, int take, CancellationToken cancellationToken);
    void SetExpectedRowVersion(VisitorRequest request, byte[] rowVersion);
    void Remove(VisitorRequest request);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
