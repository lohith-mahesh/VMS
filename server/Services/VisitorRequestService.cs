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

public sealed class VisitorRequestService(RrvmsDbContext dbContext) : IVisitorRequestService
{
    public async Task<(IReadOnlyList<VisitorRequestListItemDto> Items, int Total)> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = dbContext.VisitorRequests.AsNoTracking().Include(request => request.Visitor).OrderByDescending(request => request.CreatedAt);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(request => new VisitorRequestListItemDto(request.Id, request.RequestNumber, request.Visitor.FullName, request.Visitor.CompanyName, request.CurrentStatus, request.CreatedAt)).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<VisitorRequestDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var request = await dbContext.VisitorRequests.AsNoTracking().Include(item => item.Visitor).Include(item => item.VisitDays).Include(item => item.Assets).Include(item => item.EcReviews).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (request is null) return null;
        var audit = await dbContext.AuditLogs.AsNoTracking().Where(log => log.EntityType == nameof(VisitorRequest) && log.EntityId == id).OrderByDescending(log => log.CreatedAt).Select(log => new AuditDto(log.Id, log.Action, log.EntityType, log.EntityId, log.Details, log.CreatedAt)).ToListAsync(cancellationToken);
        return ToDetail(request, audit);
    }

    public async Task<VisitorRequestDetailDto> CreateAsync(CreateVisitorRequestDto input, string requesterKey, CancellationToken cancellationToken)
    {
        if (input.VisitDays.Count == 0 || input.VisitDays.Any(day => day.VisitDate < DateOnly.FromDateTime(DateTime.UtcNow))) throw new ArgumentException("At least one future visit date is required.");
        if (input.VisitDays.GroupBy(day => day.VisitDate).Any(group => group.Count() > 1)) throw new ArgumentException("Visit dates must be unique.");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        if (!Enum.TryParse<VisitorType>(input.VisitorType, true, out var visitorType)) throw new ArgumentException("Visitor type must be Internal or External.");
        var visitor = new Visitor { Id = Guid.NewGuid(), FullName = input.FullName.Trim(), CompanyName = input.CompanyName.Trim(), Citizenship = input.Citizenship.Trim(), Country = input.Country.Trim(), Designation = input.Designation.Trim(), Email = input.Email.Trim(), Phone = input.Phone.Trim(), IdType = input.IdType.Trim(), IdLast4 = input.IdLast4, VisitorType = visitorType, CreatedAt = now, UpdatedAt = now };
        var request = new VisitorRequest { Id = Guid.NewGuid(), RequestNumber = await NextRequestNumber(cancellationToken), Visitor = visitor, RequesterId = StableGuid(requesterKey), MainHostId = StableGuid(requesterKey), Purpose = input.Purpose.Trim(), VisitingCompany = input.VisitingCompany.Trim(), VisitingSite = input.VisitingSite.Trim(), VisitPurposeType = input.VisitPurposeType.Trim(), CurrentStatus = WorkflowStatus.Submitted, CreatedAt = now, UpdatedAt = now, SubmittedAt = now };
        request.VisitDays = input.VisitDays.Select(day => new VisitDay { Id = Guid.NewGuid(), VisitDate = day.VisitDate, ExpectedArrivalTime = day.ExpectedArrivalTime, ExpectedDepartureTime = day.ExpectedDepartureTime, Status = VisitDayStatus.Expected, CreatedAt = now, UpdatedAt = now }).ToList();
        request.Assets = input.Assets.Select(asset => new Asset { Id = Guid.NewGuid(), AssetType = asset.AssetType.Trim(), Description = asset.Description.Trim(), SerialNumber = asset.SerialNumber.Trim(), IsDeclared = true, VerificationStatus = AssetVerificationStatus.NotVerified, CreatedAt = now, UpdatedAt = now }).ToList();
        dbContext.VisitorRequests.Add(request);
        dbContext.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), Action = "REQUEST_CREATED", EntityType = nameof(VisitorRequest), EntityId = request.Id, PerformedByUserId = StableGuid(requesterKey), Details = request.RequestNumber, CreatedAt = now });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (await GetAsync(request.Id, cancellationToken))!;
    }

    public async Task<VisitorRequestDetailDto> ExecuteActionAsync(Guid requestId, WorkflowActionDto input, string userId, string role, CancellationToken cancellationToken)
    {
        var request = await dbContext.VisitorRequests.Include(item => item.Visitor).Include(item => item.VisitDays).Include(item => item.Assets).FirstOrDefaultAsync(item => item.Id == requestId, cancellationToken) ?? throw new KeyNotFoundException("Visitor request was not found.");
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var action = input.Action.Trim().ToLowerInvariant();
        var oldStatus = request.CurrentStatus;
        var now = DateTimeOffset.UtcNow;
        var actor = StableGuid(userId);
        var actorRole = role.ToLowerInvariant();

        void RequireRole(params string[] roles) { if (!roles.Contains(actorRole, StringComparer.OrdinalIgnoreCase) && actorRole != "admin") throw new UnauthorizedAccessException("Your role cannot perform this action."); }
        void RequireStatus(params string[] statuses) { if (!statuses.Contains(request.CurrentStatus, StringComparer.OrdinalIgnoreCase)) throw new InvalidOperationException($"Action is not valid while the request is {request.CurrentStatus}."); }
        VisitDay GetDay() => request.VisitDays.FirstOrDefault(day => input.VisitDayId is null || day.Id == input.VisitDayId) ?? throw new InvalidOperationException("A valid visit day is required.");

        switch (action)
        {
            case "visitor-submit":
                RequireRole("requester", "host"); RequireStatus(WorkflowStatus.Submitted); request.CurrentStatus = WorkflowStatus.HostReview; break;
            case "host-submit":
                RequireRole("requester", "host"); RequireStatus(WorkflowStatus.HostReview, WorkflowStatus.Submitted); request.CurrentStatus = request.Visitor.VisitorType == VisitorType.Internal ? WorkflowStatus.EcReview : WorkflowStatus.PendingDps; break;
            case "dps":
                RequireRole("requester", "host", "exportcontrol"); RequireStatus(WorkflowStatus.PendingDps, WorkflowStatus.HostDps, WorkflowStatus.EcDpsReview); if (!Enum.TryParse<DpsResult>(input.Comment, true, out var dpsResult)) throw new InvalidOperationException("Comment must be Clear or Flagged for a DPS action."); dbContext.DPSRecords.Add(new DPSRecord { Id = Guid.NewGuid(), VisitorRequestId = request.Id, PerformedByUserId = actor, PerformedByType = actorRole == "exportcontrol" ? DpsPerformedByType.ExportControl : DpsPerformedByType.Host, Status = DpsStatus.Completed, Result = dpsResult, PerformedAt = now, Notes = input.Reason }); request.CurrentStatus = dpsResult == DpsResult.Flagged ? WorkflowStatus.EcReview : WorkflowStatus.EcReview; break;
            case "ec-approve":
                RequireRole("exportcontrol"); RequireStatus(WorkflowStatus.EcReview, WorkflowStatus.DocumentationSubmitted, WorkflowStatus.EcReReviewRequired, WorkflowStatus.SecurityHold); dbContext.ECReviews.Add(new ECReview { Id = Guid.NewGuid(), VisitorRequestId = request.Id, ReviewerId = actor, Status = EcReviewStatus.Approved, Decision = EcDecision.Approve, Comments = input.Comment ?? string.Empty, ReviewedAt = now, CreatedAt = now }); request.CurrentStatus = WorkflowStatus.Approved; foreach (var day in request.VisitDays.Where(day => day.Status is VisitDayStatus.Expected or VisitDayStatus.OnHold)) day.Status = VisitDayStatus.Approved; AddNotification(request, "EC_APPROVED", "Visitor request approved by Export Control.", now); break;
            case "ec-reject":
                RequireRole("exportcontrol"); RequireStatus(WorkflowStatus.EcReview, WorkflowStatus.DocumentationSubmitted, WorkflowStatus.EcReReviewRequired, WorkflowStatus.SecurityHold); if (string.IsNullOrWhiteSpace(input.Reason)) throw new ArgumentException("A rejection reason is required."); dbContext.ECReviews.Add(new ECReview { Id = Guid.NewGuid(), VisitorRequestId = request.Id, ReviewerId = actor, Status = EcReviewStatus.Rejected, Decision = EcDecision.Reject, Comments = input.Reason, ReviewedAt = now, CreatedAt = now }); request.CurrentStatus = WorkflowStatus.Rejected; request.RejectionReason = input.Reason; AddNotification(request, "EC_REJECTED", "Visitor request was rejected by Export Control.", now); break;
            case "ec-request-documents":
                RequireRole("exportcontrol"); RequireStatus(WorkflowStatus.EcReview, WorkflowStatus.EcReReviewRequired); if (string.IsNullOrWhiteSpace(input.Reason)) throw new ArgumentException("Required documentation must be specified."); dbContext.ECReviews.Add(new ECReview { Id = Guid.NewGuid(), VisitorRequestId = request.Id, ReviewerId = actor, Status = EcReviewStatus.PendingDocumentation, Decision = EcDecision.RequestDocumentation, RequestedDocuments = input.Reason, Comments = input.Comment ?? string.Empty, CreatedAt = now }); request.CurrentStatus = WorkflowStatus.PendingDocumentation; AddNotification(request, "DOCUMENTATION_REQUESTED", "Additional documentation is required.", now); break;
            case "submit-documents":
                RequireRole("requester", "host"); RequireStatus(WorkflowStatus.PendingDocumentation); dbContext.Documents.Add(new Document { Id = Guid.NewGuid(), VisitorRequestId = request.Id, UploadedByUserId = actor, DocumentType = "Supporting documentation", FileName = input.Comment ?? "submitted-document", StorageReference = input.Comment ?? "metadata-only", UploadedAt = now }); request.CurrentStatus = WorkflowStatus.DocumentationSubmitted; break;
            case "resolve-hold":
                RequireRole("requester", "host"); RequireStatus(WorkflowStatus.SecurityHold); dbContext.Documents.Add(new Document { Id = Guid.NewGuid(), VisitorRequestId = request.Id, UploadedByUserId = actor, DocumentType = "Security hold resolution", FileName = "hold-resolution", StorageReference = input.Comment ?? "resolution-metadata", UploadedAt = now }); request.CurrentStatus = WorkflowStatus.EcReview; break;
            case "host-change":
                RequireRole("requester", "host"); if (string.IsNullOrWhiteSpace(input.NewUserId)) throw new ArgumentException("A new main host is required."); request.MainHostId = StableGuid(input.NewUserId); if (oldStatus == WorkflowStatus.Approved) request.CurrentStatus = WorkflowStatus.EcReReviewRequired; AddNotification(request, "HOST_CHANGED", "Main host changed; Export Control review is required.", now); break;
            case "escort-change":
                RequireRole("requester", "host"); request.AccompanyingEmployeeId = string.IsNullOrWhiteSpace(input.NewUserId) ? null : StableGuid(input.NewUserId); break;
            case "verify":
                RequireRole("security"); RequireStatus(WorkflowStatus.Approved); if (input.IdLast4 != request.Visitor.IdLast4) { request.CurrentStatus = WorkflowStatus.EntryRejected; throw new InvalidOperationException("Identity mismatch. Entry rejected."); } request.CurrentStatus = WorkflowStatus.SecurityVerification; break;
            case "hold":
                RequireRole("security"); RequireStatus(WorkflowStatus.SecurityVerification, WorkflowStatus.Approved); var heldDay = GetDay(); heldDay.Status = VisitDayStatus.OnHold; request.Assets.Add(new Asset { Id = Guid.NewGuid(), VisitorRequestId = request.Id, AssetType = "Undeclared asset", Description = input.Comment ?? "Detected by Security", SerialNumber = input.AssetSerials ?? string.Empty, IsDeclared = false, VerificationStatus = AssetVerificationStatus.Undeclared, DetectedAt = now, CreatedAt = now, UpdatedAt = now }); request.CurrentStatus = WorkflowStatus.SecurityHold; AddNotification(request, "SECURITY_HOLD", "Undeclared asset requires Export Control review.", now); break;
            case "check-in":
                RequireRole("security"); RequireStatus(WorkflowStatus.SecurityVerification, WorkflowStatus.Approved); var checkInDay = GetDay(); if (string.IsNullOrWhiteSpace(input.BadgeNumber)) throw new ArgumentException("Badge number is required."); var badge = new Badge { Id = Guid.NewGuid(), BadgeNumber = input.BadgeNumber, VisitorId = request.VisitorId, VisitDayId = checkInDay.Id, IssuedByUserId = actor, IssuedAt = now, Status = BadgeStatus.Issued }; dbContext.Badges.Add(badge); dbContext.VisitCheckIns.Add(new VisitCheckIn { Id = Guid.NewGuid(), VisitDayId = checkInDay.Id, BadgeId = badge.Id, SecurityUserId = actor, PhysicalIdVerified = true, AssetsVerified = true, CheckedInAt = now }); checkInDay.ActualArrivalTime = now; checkInDay.Status = VisitDayStatus.CheckedIn; request.CurrentStatus = WorkflowStatus.CheckedIn; break;
            case "check-out":
                RequireRole("security"); RequireStatus(WorkflowStatus.CheckedIn); var checkOutDay = GetDay(); var issuedBadge = await dbContext.Badges.FirstOrDefaultAsync(item => item.VisitDayId == checkOutDay.Id && item.Status == BadgeStatus.Issued, cancellationToken) ?? throw new InvalidOperationException("No issued badge exists for this visit day."); issuedBadge.Status = BadgeStatus.Returned; issuedBadge.ReturnedAt = now; dbContext.VisitCheckOuts.Add(new VisitCheckOut { Id = Guid.NewGuid(), VisitDayId = checkOutDay.Id, BadgeId = issuedBadge.Id, SecurityUserId = actor, BadgeReturned = true, Notes = input.Comment ?? string.Empty, CheckedOutAt = now }); checkOutDay.ActualDepartureTime = now; checkOutDay.Status = VisitDayStatus.Completed; request.CurrentStatus = request.VisitDays.Any(day => day.VisitDate > checkOutDay.VisitDate && day.Status == VisitDayStatus.Approved) ? WorkflowStatus.Approved : WorkflowStatus.Completed; break;
            case "no-show":
                RequireRole("security", "admin"); var noShowDay = GetDay(); if (noShowDay.Status == VisitDayStatus.CheckedIn || noShowDay.Status == VisitDayStatus.Completed) throw new InvalidOperationException("A completed visit cannot be marked no-show."); noShowDay.Status = VisitDayStatus.NoShow; noShowDay.NoShowMarkedAt = now; AddNotification(request, "NO_SHOW", "Visitor marked as no-show.", now); break;
            default: throw new InvalidOperationException("Unknown workflow action.");
        }

        request.UpdatedAt = now;
        dbContext.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), Action = action.ToUpperInvariant(), EntityType = nameof(VisitorRequest), EntityId = request.Id, PerformedByUserId = actor, Details = $"{oldStatus} -> {request.CurrentStatus}{(string.IsNullOrWhiteSpace(input.Comment) ? string.Empty : $": {input.Comment}")}{(string.IsNullOrWhiteSpace(input.NewUserId) ? string.Empty : $"; user={input.NewUserId}")}", CreatedAt = now });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (await GetAsync(request.Id, cancellationToken))!;
    }

    private void AddNotification(VisitorRequest request, string type, string message, DateTimeOffset now)
    {
        dbContext.Notifications.Add(new Notification { Id = Guid.NewGuid(), UserId = request.MainHostId, Type = type, Message = message, CreatedAt = now });
        if (type is "EC_APPROVED" or "SECURITY_HOLD") dbContext.Notifications.Add(new Notification { Id = Guid.NewGuid(), UserId = StableGuid("prototype-security"), Type = type, Message = message, CreatedAt = now });
    }

    private async Task<string> NextRequestNumber(CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"RRVMS-{year}-";
        var count = await dbContext.VisitorRequests.CountAsync(request => request.RequestNumber.StartsWith(prefix), cancellationToken);
        var candidate = $"{prefix}{count + 1:000000}";
        while (await dbContext.VisitorRequests.AnyAsync(request => request.RequestNumber == candidate, cancellationToken)) candidate = $"{prefix}{++count:000000}";
        return candidate;
    }

    private static Guid StableGuid(string value)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(bytes);
    }

    private static VisitorRequestDetailDto ToDetail(VisitorRequest request, IReadOnlyList<AuditDto> audit) => new(request.Id, request.RequestNumber, new VisitorDto(request.Visitor.Id, request.Visitor.FullName, request.Visitor.CompanyName, request.Visitor.Citizenship, request.Visitor.Country, request.Visitor.Designation, request.Visitor.Email, request.Visitor.Phone, request.Visitor.IdType, request.Visitor.IdLast4, request.Visitor.VisitorType.ToString()), request.Purpose, request.VisitingCompany, request.VisitingSite, request.VisitPurposeType, request.CurrentStatus, request.VisitDays.Select(day => new VisitDayDto(day.Id, day.VisitDate, day.ExpectedArrivalTime, day.ExpectedDepartureTime, day.Status.ToString(), day.ActualArrivalTime, day.ActualDepartureTime)).ToList(), request.Assets.Select(asset => new AssetDto(asset.Id, asset.AssetType, asset.Description, asset.SerialNumber, asset.IsDeclared, asset.IsVerified, asset.VerificationStatus.ToString())).ToList(), audit);
}
