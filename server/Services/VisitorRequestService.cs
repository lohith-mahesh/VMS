using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RRVMS.Api.Data;
using RRVMS.Api.DTOs;
using RRVMS.Api.Models;

namespace RRVMS.Api.Services;

public interface IVisitorRequestService
{
    Task<(IReadOnlyList<VisitorRequestListItemDto> Items, int Total)> ListAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<VisitorRequestDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<VisitorRequestDetailDto> CreateAsync(CreateVisitorRequestDto input, string requesterKey, CancellationToken cancellationToken);
    Task<VisitorRequestDetailDto> ExecuteActionAsync(Guid requestId, WorkflowActionDto input, string userId, string role, CancellationToken cancellationToken);
}

public sealed class VisitorRequestService(RrvmsDbContext db) : IVisitorRequestService
{
    public async Task<(IReadOnlyList<VisitorRequestListItemDto> Items, int Total)> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        var query = db.VisitorRequests.AsNoTracking().Include(r => r.Visitor).OrderByDescending(r => r.CreatedAt);
        var total = await query.CountAsync(ct); var size = Math.Clamp(pageSize, 1, 100);
        var items = await query.Skip((Math.Max(page, 1) - 1) * size).Take(size).Select(r => new VisitorRequestListItemDto(r.Id, r.RequestNumber, r.Visitor.FullName, r.VisitingCompany, r.Status.ToString(), r.CreatedAt)).ToListAsync(ct);
        return (items, total);
    }

    public async Task<VisitorRequestDetailDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var request = await db.VisitorRequests.AsNoTracking().Include(r => r.Visitor).Include(r => r.VisitDays).Include(r => r.Assets).FirstOrDefaultAsync(r => r.Id == id, ct);
        if (request is null) return null;
        var audit = await db.AuditLogs.AsNoTracking().Where(log => log.EntityType == nameof(VisitorRequest) && log.EntityId == id).OrderByDescending(log => log.CreatedAt).Select(log => new AuditDto(log.Id, log.Action, log.EntityType, log.EntityId, log.Details, log.CreatedAt)).ToListAsync(ct);
        return ToDetail(request, audit);
    }

    public async Task<VisitorRequestDetailDto> CreateAsync(CreateVisitorRequestDto input, string requesterKey, CancellationToken ct)
    {
        if (!Enum.TryParse<VisitorType>(input.VisitorType, true, out var visitorType)) throw new ArgumentException("Visitor type must be Internal or External.");
        if (input.VisitDays.Count == 0 || input.VisitDays.Any(d => d.VisitDate < DateOnly.FromDateTime(DateTime.UtcNow))) throw new ArgumentException("At least one future visit date is required.");
        if (input.VisitDays.GroupBy(d => d.VisitDate).Any(g => g.Count() > 1)) throw new ArgumentException("Visit dates must be unique.");
        var now = DateTimeOffset.UtcNow;
        var blankVisitor = new Visitor { Id = Guid.NewGuid(), VisitorType = visitorType, CreatedAt = now, UpdatedAt = now };
        var request = new VisitorRequest { Id = Guid.NewGuid(), RequestNumber = await NextRequestNumber(ct), Status = RequestStatus.VISITOR_FORM_PENDING, Visitor = blankVisitor, RequesterId = StableGuid(requesterKey), MainHostId = StableGuid(input.MainHostId), EscortingHostId = string.IsNullOrWhiteSpace(input.EscortingHostId) ? null : StableGuid(input.EscortingHostId), VisitorType = visitorType, VisitingCompany = input.VisitingCompany.Trim(), VisitingSite = input.VisitingSite.Trim(), AreasToVisit = input.AreasToVisit.Trim(), SiteTimezone = input.SiteTimezone.Trim(), NumberOfVisitors = input.NumberOfVisitors, VisitPurposeType = input.VisitPurposeType.Trim(), CreatedAt = now, UpdatedAt = now };
        request.VisitorForm = new VisitorForm { Id = Guid.NewGuid(), Visitor = blankVisitor, Status = "PENDING", CreatedAt = now, UpdatedAt = now };
        request.VisitDays = input.VisitDays.Select(d => new VisitDay { Id = Guid.NewGuid(), VisitDate = d.VisitDate, ExpectedArrivalTime = d.ExpectedArrivalTime, ExpectedDepartureTime = d.ExpectedDepartureTime, CreatedAt = now, UpdatedAt = now }).ToList();
        db.VisitorRequests.Add(request); db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), Action = "REQUEST_CREATED", EntityType = nameof(VisitorRequest), EntityId = request.Id, PerformedByUserId = StableGuid(requesterKey), Details = request.RequestNumber, CreatedAt = now });
        await db.SaveChangesAsync(ct); return (await GetAsync(request.Id, ct))!;
    }

    public async Task<VisitorRequestDetailDto> ExecuteActionAsync(Guid id, WorkflowActionDto input, string userId, string role, CancellationToken ct)
    {
        var request = await db.VisitorRequests.Include(r => r.Visitor).Include(r => r.VisitDays).Include(r => r.Assets).FirstOrDefaultAsync(r => r.Id == id, ct) ?? throw new KeyNotFoundException("Visitor request was not found.");
        var actor = StableGuid(userId); var action = input.Action.Trim().ToLowerInvariant(); var old = request.Status; var now = DateTimeOffset.UtcNow;
        void Role(params string[] allowed) { if (!allowed.Contains(role, StringComparer.OrdinalIgnoreCase)) throw new UnauthorizedAccessException("Your role cannot perform this action."); }
        void State(params RequestStatus[] allowed) { if (!allowed.Contains(request.Status)) throw new InvalidOperationException($"Action is not valid while the request is {request.Status}."); }
        switch (action)
        {
            case "visitor-submit": Role("HOST_REQUESTER"); State(RequestStatus.VISITOR_FORM_PENDING); request.Status = RequestStatus.VISITOR_FORM_SUBMITTED; break;
            case "host-submit": Role("HOST_REQUESTER"); State(RequestStatus.VISITOR_FORM_SUBMITTED, RequestStatus.HOST_REVIEW); request.Status = request.VisitorType == VisitorType.Internal ? RequestStatus.EC_REVIEW : RequestStatus.HOST_DPS; break;
            case "ec-approve": Role("EXPORT_CONTROL"); State(RequestStatus.EC_REVIEW, RequestStatus.DOCUMENTATION_SUBMITTED, RequestStatus.EC_RE_REVIEW_REQUIRED); request.Status = RequestStatus.APPROVED; request.ApprovedAt = now; break;
            case "ec-reject": Role("EXPORT_CONTROL"); State(RequestStatus.EC_REVIEW, RequestStatus.DOCUMENTATION_SUBMITTED, RequestStatus.EC_RE_REVIEW_REQUIRED); if (string.IsNullOrWhiteSpace(input.Reason)) throw new ArgumentException("A rejection reason is required."); request.Status = RequestStatus.REJECTED; request.RejectionReason = input.Reason; break;
            case "ec-request-documents": Role("EXPORT_CONTROL"); State(RequestStatus.EC_REVIEW, RequestStatus.EC_RE_REVIEW_REQUIRED); if (string.IsNullOrWhiteSpace(input.Reason)) throw new ArgumentException("Required information must be specified."); request.Status = RequestStatus.PENDING_DOCUMENTATION; db.Add(new AdditionalInformationRequest { Id = Guid.NewGuid(), VisitorRequestId = id, RequestedByUserId = actor, RequestedFields = input.Reason, RequestComment = input.Comment ?? input.Reason, CreatedAt = now, UpdatedAt = now }); break;
            case "host-change": Role("HOST_REQUESTER"); if (string.IsNullOrWhiteSpace(input.NewUserId)) throw new ArgumentException("A new main host is required."); request.PreviousMainHostId = request.MainHostId; request.MainHostId = StableGuid(input.NewUserId); request.MainHostChangedAt = now; if (request.Status == RequestStatus.APPROVED) request.Status = RequestStatus.EC_RE_REVIEW_REQUIRED; break;
            case "escort-change": Role("HOST_REQUESTER"); request.EscortingHostId = string.IsNullOrWhiteSpace(input.NewUserId) ? null : StableGuid(input.NewUserId); break;
            case "no-show": Role("RECEPTION"); var day = request.VisitDays.FirstOrDefault(d => d.Id == input.VisitDayId) ?? throw new ArgumentException("A valid visit day is required."); day.Status = VisitDayStatus.NO_SHOW; day.NoShowMarkedAt = now; break;
            default: throw new InvalidOperationException("Unknown workflow action.");
        }
        request.UpdatedAt = now; db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), Action = action.ToUpperInvariant(), EntityType = nameof(VisitorRequest), EntityId = id, PerformedByUserId = actor, Details = $"{old} -> {request.Status}: {input.Comment ?? input.Reason}", CreatedAt = now }); await db.SaveChangesAsync(ct); return (await GetAsync(id, ct))!;
    }

    private async Task<string> NextRequestNumber(CancellationToken ct) { var prefix = $"RRVMS-{DateTime.UtcNow.Year}-"; var count = await db.VisitorRequests.CountAsync(r => r.RequestNumber.StartsWith(prefix), ct); return $"{prefix}{count + 1:000000}"; }
    private static Guid StableGuid(string value) => new(MD5.HashData(Encoding.UTF8.GetBytes(value)));
    private static VisitorRequestDetailDto ToDetail(VisitorRequest r, IReadOnlyList<AuditDto> audit) => new(r.Id, r.RequestNumber, new VisitorDto(r.Visitor.Id, r.Visitor.FullName, r.Visitor.CompanyName, r.Visitor.Citizenship, r.Visitor.Country, r.Visitor.Designation, r.Visitor.Email, r.Visitor.Phone, r.Visitor.IdType, r.Visitor.IdLast4, r.Visitor.VisitorType.ToString()), r.VisitPurposeType, r.VisitingCompany, r.VisitingSite, r.VisitPurposeType, r.Status.ToString(), r.VisitDays.Select(d => new VisitDayDto(d.Id, d.VisitDate, d.ExpectedArrivalTime, d.ExpectedDepartureTime, d.Status.ToString(), d.ActualArrivalTime, d.ActualDepartureTime)).ToList(), r.Assets.Select(a => new AssetDto(a.Id, a.AssetType, a.Description, a.SerialNumber, a.IsDeclared, a.IsVerified, a.VerificationStatus.ToString())).ToList(), audit);
}
