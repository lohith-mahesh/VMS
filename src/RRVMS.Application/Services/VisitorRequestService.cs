using System.Text.Json;
using RRVMS.Application.Abstractions;
using RRVMS.Application.Common;
using RRVMS.Application.Contracts;
using RRVMS.Application.Mapping;
using RRVMS.Application.Validation;
using RRVMS.Domain.Common;
using RRVMS.Domain.Entities;
using RRVMS.Domain.Enums;
using RRVMS.Domain.ValueObjects;

namespace RRVMS.Application.Services;

public sealed class VisitorRequestService(
    IVisitorRequestRepository repository,
    IDocumentStorage documentStorage,
    IClock clock,
    ICurrentActor actor,
    TimeZoneInfo businessTimeZone)
{
    private const long MaximumPdfSize = 10 * 1024 * 1024;

    public async Task<RequestDetailResponse> CreateAsync(CreateVisitorRequestCommand command, CancellationToken cancellationToken)
    {
        EnsureRole(UserRole.HostRequester);
        RequestValidator.Validate(command);
        var now = clock.UtcNow;
        var localNow = TimeZoneInfo.ConvertTime(now, businessTimeZone);
        var window = VisitWindow.Create(
            TimeZoneInfo.ConvertTime(command.VisitStart, businessTimeZone),
            TimeZoneInfo.ConvertTime(command.VisitEnd, businessTimeZone),
            now);
        var sequence = await repository.NextSequenceAsync(localNow.Year, localNow.Month, cancellationToken);
        var request = VisitorRequest.Create(
            RequestNumber.Create(localNow.Year, localNow.Month, sequence),
            actor.ObjectId,
            command.MainHostObjectId,
            command.MainHostName,
            command.HostDepartment,
            command.EscortingHostObjectId,
            command.EscortingHostName,
            command.VisitorType,
            command.ContractorType,
            command.SiteCode,
            command.PurposeType,
            command.Purpose,
            command.AreasToVisit,
            window,
            command.NumberOfVisitors,
            now);

        request.RecordAudit("RequestCreated", actor.ObjectId, actor.Role.ToString(), null, ResponseMapper.ToAuditSnapshot(request), "Visitor request created.", actor.CorrelationId, now);
        await repository.AddAsync(request, cancellationToken);
        await AddNotificationAsync("RequestCreated", request, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ResponseMapper.ToDetail(request, true);
    }

    public async Task<PagedResponse<RequestSummaryResponse>> ListAsync(
        string? search,
        VisitorRequestStatus? status,
        string? siteCode,
        DateOnly? visitDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        EnsureAuthenticated();
        ValidateFilters(search, siteCode);
        page = Math.Clamp(page, 1, 1000000);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = BuildRoleQuery(search, status, siteCode, visitDate, page, pageSize);
        var requests = await repository.ListAsync(query, cancellationToken);
        var total = await repository.CountAsync(query, cancellationToken);
        return new PagedResponse<RequestSummaryResponse>(requests.Select(ResponseMapper.ToSummary).ToArray(), page, pageSize, total);
    }

    public async Task<RequestDetailResponse> GetAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var request = await GetAuthorizedAsync(requestId, cancellationToken);
        return ResponseMapper.ToDetail(request, actor.Role != UserRole.Reception);
    }

    public async Task<RequestDetailResponse> SubmitVisitorAsync(Guid requestId, Guid visitorId, SubmitVisitorCommand command, CancellationToken cancellationToken)
    {
        EnsureRole(UserRole.HostRequester);
        RequestValidator.Validate(command);
        RequestValidator.ValidateRowVersion(command.RowVersion);
        var request = await GetHostRequestAsync(requestId, cancellationToken);
        repository.SetExpectedRowVersion(request, command.RowVersion);
        var before = ResponseMapper.ToAuditSnapshot(request);
        var visitor = request.Visitor(visitorId);
        var correction = visitor.DetailsStatus == VisitorDetailsStatus.RevisionRequired;
        var details = new VisitorDetails(
            command.FirstName,
            command.MiddleName,
            command.LastName,
            command.Citizenship,
            command.Designation,
            command.CompanyName,
            command.CompanyAddress,
            command.OfficeCity,
            command.OfficeCountry,
            command.PhoneCountry,
            command.PhoneDialCode,
            command.Telephone,
            command.Email,
            command.IdType,
            command.OtherIdType,
            command.Assets.Select(item => new AssetDetails(item.AssetType, item.Description, item.SerialNumber)).ToArray());

        var now = clock.UtcNow;
        request.SubmitVisitor(visitorId, details, actor.ObjectId, now);
        if (correction)
        {
            request.ResolveCorrections(visitorId, JsonSerializer.Serialize(ResponseMapper.ToAuditSnapshot(request)), now);
        }

        request.RecordAudit(
            correction ? "VisitorCorrectionSubmitted" : "VisitorDetailsSubmitted",
            actor.ObjectId,
            actor.Role.ToString(),
            before,
            ResponseMapper.ToAuditSnapshot(request),
            $"Visitor {visitor.Sequence} details submitted.",
            actor.CorrelationId,
            now);
        await AddNotificationAsync(correction ? "CorrectionSubmitted" : "VisitorDetailsSubmitted", request, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ResponseMapper.ToDetail(request, true);
    }

    public async Task<RequestDetailResponse> ReviseRequestAsync(Guid requestId, ReviseRequestCommand command, CancellationToken cancellationToken)
    {
        EnsureRole(UserRole.HostRequester);
        RequestValidator.Validate(command);
        RequestValidator.ValidateRowVersion(command.RowVersion);
        var request = await GetHostRequestAsync(requestId, cancellationToken);
        repository.SetExpectedRowVersion(request, command.RowVersion);
        var before = ResponseMapper.ToAuditSnapshot(request);
        var now = clock.UtcNow;
        request.ReviseRequestDetails(
            command.SiteCode,
            command.PurposeType,
            command.Purpose,
            command.AreasToVisit,
            command.MainHostObjectId,
            command.MainHostName,
            command.HostDepartment,
            command.EscortingHostObjectId,
            command.EscortingHostName,
            now);
        request.RecordAudit("RequestDetailsRevised", actor.ObjectId, actor.Role.ToString(), before, ResponseMapper.ToAuditSnapshot(request), "Request-level correction submitted. Screening decisions were reset.", actor.CorrelationId, now);
        await repository.SaveChangesAsync(cancellationToken);
        return ResponseMapper.ToDetail(request, true);
    }

    public async Task<RequestDetailResponse> UploadDpsAsync(
        Guid requestId,
        Guid visitorId,
        string fileName,
        string contentType,
        Stream content,
        byte[] rowVersion,
        CancellationToken cancellationToken)
    {
        EnsureRole(UserRole.HostRequester);
        RequestValidator.ValidateRowVersion(rowVersion);
        var safeFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeFileName) || safeFileName.Length > 255
            || !string.Equals(Path.GetExtension(safeFileName), ".pdf", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException(new Dictionary<string, string[]> { ["file"] = ["Upload a PDF file."] });
        }

        await using var buffered = new MemoryStream();
        await content.CopyToAsync(buffered, cancellationToken);
        if (buffered.Length is 0 or > MaximumPdfSize)
        {
            throw new ValidationException(new Dictionary<string, string[]> { ["file"] = ["The PDF must be between 1 byte and 10 MB."] });
        }

        var signature = buffered.GetBuffer().AsSpan(0, Math.Min(5, (int)buffered.Length));
        if (!signature.SequenceEqual("%PDF-"u8))
        {
            throw new ValidationException(new Dictionary<string, string[]> { ["file"] = ["The uploaded file is not a valid PDF."] });
        }

        buffered.Position = 0;
        var request = await GetHostRequestAsync(requestId, cancellationToken);
        repository.SetExpectedRowVersion(request, rowVersion);
        var visitor = request.Visitor(visitorId);
        var before = ResponseMapper.ToAuditSnapshot(request);
        var version = visitor.DpsDocuments.Count + 1;
        var stored = await documentStorage.SavePdfAsync(requestId, visitorId, version, buffered, safeFileName, cancellationToken);
        try
        {
            var now = clock.UtcNow;
            request.AttachDps(visitorId, stored.BlobName, safeFileName, stored.Size, stored.Sha256, actor.ObjectId, now);
            request.RecordAudit("DpsUploaded", actor.ObjectId, actor.Role.ToString(), before, ResponseMapper.ToAuditSnapshot(request), $"DPS metadata recorded for visitor {visitor.Sequence}, version {version}.", actor.CorrelationId, now);
            await repository.SaveChangesAsync(cancellationToken);
            return ResponseMapper.ToDetail(request, true);
        }
        catch
        {
            await documentStorage.DeleteAsync(stored.BlobName, cancellationToken);
            throw;
        }
    }

    public async Task<RequestDetailResponse> SubmitForScreeningAsync(Guid requestId, SubmitForScreeningCommand command, CancellationToken cancellationToken)
    {
        EnsureRole(UserRole.HostRequester);
        RequestValidator.ValidateRowVersion(command.RowVersion);
        var request = await GetHostRequestAsync(requestId, cancellationToken);
        repository.SetExpectedRowVersion(request, command.RowVersion);
        var before = ResponseMapper.ToAuditSnapshot(request);
        var now = clock.UtcNow;
        request.SubmitForScreening(now);
        request.RecordAudit("SubmittedForScreening", actor.ObjectId, actor.Role.ToString(), before, ResponseMapper.ToAuditSnapshot(request), "Request submitted to Export Control.", actor.CorrelationId, now);
        await AddNotificationAsync("SubmittedForScreening", request, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ResponseMapper.ToDetail(request, true);
    }

    public async Task<RequestDetailResponse> ReviewAsync(Guid requestId, ReviewVisitorsCommand command, CancellationToken cancellationToken)
    {
        EnsureRole(UserRole.ExportControl);
        RequestValidator.Validate(command);
        RequestValidator.ValidateRowVersion(command.RowVersion);
        var request = await GetAuthorizedAsync(requestId, cancellationToken);
        repository.SetExpectedRowVersion(request, command.RowVersion);
        var before = ResponseMapper.ToAuditSnapshot(request);
        var now = clock.UtcNow;
        request.ReviewVisitors(command.VisitorIds, command.Decision, command.Classification, command.Comments, actor.ObjectId, now);
        request.RecordAudit("ScreeningDecisionRecorded", actor.ObjectId, actor.Role.ToString(), before, ResponseMapper.ToAuditSnapshot(request), $"{command.Decision} recorded for {command.VisitorIds.Distinct().Count()} visitor(s).", actor.CorrelationId, now);
        await AddNotificationAsync("ScreeningDecisionRecorded", request, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ResponseMapper.ToDetail(request, true);
    }

    public async Task<RequestDetailResponse> RequestCorrectionAsync(Guid requestId, RequestCorrectionCommand command, CancellationToken cancellationToken)
    {
        EnsureRole(UserRole.ExportControl);
        RequestValidator.Validate(command);
        RequestValidator.ValidateRowVersion(command.RowVersion);
        var request = await GetAuthorizedAsync(requestId, cancellationToken);
        repository.SetExpectedRowVersion(request, command.RowVersion);
        var before = ResponseMapper.ToAuditSnapshot(request);
        var now = clock.UtcNow;
        request.RequestCorrection(
            command.VisitorId,
            JsonSerializer.Serialize(command.Fields.Distinct(StringComparer.OrdinalIgnoreCase)),
            command.Instructions,
            JsonSerializer.Serialize(before),
            actor.ObjectId,
            now);
        request.RecordAudit("CorrectionRequested", actor.ObjectId, actor.Role.ToString(), before, ResponseMapper.ToAuditSnapshot(request), command.Instructions, actor.CorrelationId, now);
        await AddNotificationAsync("CorrectionRequested", request, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ResponseMapper.ToDetail(request, true);
    }

    public async Task<RequestDetailResponse> RescheduleAsync(Guid requestId, RescheduleCommand command, CancellationToken cancellationToken)
    {
        EnsureRole(UserRole.HostRequester);
        RequestValidator.Validate(command);
        RequestValidator.ValidateRowVersion(command.RowVersion);
        var request = await GetHostRequestAsync(requestId, cancellationToken);
        repository.SetExpectedRowVersion(request, command.RowVersion);
        var before = ResponseMapper.ToAuditSnapshot(request);
        var now = clock.UtcNow;
        request.Reschedule(
            VisitWindow.Create(
                TimeZoneInfo.ConvertTime(command.VisitStart, businessTimeZone),
                TimeZoneInfo.ConvertTime(command.VisitEnd, businessTimeZone),
                now),
            command.Reason,
            actor.ObjectId,
            now);
        request.RecordAudit("RequestRescheduled", actor.ObjectId, actor.Role.ToString(), before, ResponseMapper.ToAuditSnapshot(request), command.Reason, actor.CorrelationId, now);
        await AddNotificationAsync("RequestRescheduled", request, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ResponseMapper.ToDetail(request, true);
    }

    public async Task<RequestDetailResponse> CancelAsync(Guid requestId, CancelRequestCommand command, CancellationToken cancellationToken)
    {
        EnsureRole(UserRole.HostRequester);
        RequestValidator.Validate(command);
        RequestValidator.ValidateRowVersion(command.RowVersion);
        var request = await GetHostRequestAsync(requestId, cancellationToken);
        repository.SetExpectedRowVersion(request, command.RowVersion);
        var before = ResponseMapper.ToAuditSnapshot(request);
        var now = clock.UtcNow;
        request.Cancel(command.Reason, now);
        request.RecordAudit("RequestCancelled", actor.ObjectId, actor.Role.ToString(), before, ResponseMapper.ToAuditSnapshot(request), command.Reason, actor.CorrelationId, now);
        await AddNotificationAsync("RequestCancelled", request, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ResponseMapper.ToDetail(request, true);
    }

    public Task<RequestDetailResponse> VerifyEntryAsync(Guid requestId, VerifyEntryCommand command, CancellationToken cancellationToken)
    {
        RequestValidator.Validate(command);
        return ApplyReceptionActionAsync(requestId, command.RowVersion, "EntryVerified", request => request.VerifyEntry(command.VisitorId, command.VisitDayId, command.Approved, command.IdentityConfirmed, command.AssetsConfirmed, command.Remarks, clock.UtcNow, businessTimeZone), cancellationToken);
    }

    public async Task<RequestDetailResponse> CheckInAsync(Guid requestId, CheckInCommand command, CancellationToken cancellationToken)
    {
        EnsureRole(UserRole.Reception);
        RequestValidator.Validate(command);
        RequestValidator.ValidateRowVersion(command.RowVersion);
        var request = await GetAuthorizedAsync(requestId, cancellationToken);
        repository.SetExpectedRowVersion(request, command.RowVersion);
        var record = request.ReceptionRecord(command.VisitorId, command.VisitDayId);
        if (await repository.ActiveBadgeExistsAsync(command.BadgeId.Trim(), record.Id, cancellationToken))
        {
            throw new ConflictException("The badge is currently assigned to another visitor.");
        }

        var before = ResponseMapper.ToAuditSnapshot(request);
        var now = clock.UtcNow;
        request.CheckIn(command.VisitorId, command.VisitDayId, command.BadgeId, now, businessTimeZone);
        request.RecordAudit("VisitorCheckedIn", actor.ObjectId, actor.Role.ToString(), before, ResponseMapper.ToAuditSnapshot(request), $"Badge {command.BadgeId.Trim()} assigned.", actor.CorrelationId, now);
        await repository.SaveChangesAsync(cancellationToken);
        return ResponseMapper.ToDetail(request, false);
    }

    public Task<RequestDetailResponse> CheckOutAsync(Guid requestId, CheckOutCommand command, CancellationToken cancellationToken) =>
        ApplyReceptionActionAsync(requestId, command.RowVersion, "VisitorCheckedOut", request => request.CheckOut(command.VisitorId, command.VisitDayId, clock.UtcNow), cancellationToken);

    public Task<RequestDetailResponse> MarkNoShowAsync(Guid requestId, MarkNoShowCommand command, CancellationToken cancellationToken) =>
        ApplyReceptionActionAsync(requestId, command.RowVersion, "VisitorMarkedNoShow", request => request.MarkNoShow(command.VisitorId, command.VisitDayId, DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.UtcNow, businessTimeZone).Date), clock.UtcNow), cancellationToken);

    public async Task<DocumentDownload> DownloadDpsAsync(Guid requestId, Guid visitorId, Guid documentId, CancellationToken cancellationToken)
    {
        EnsureRole(UserRole.ExportControl);
        var request = await GetAuthorizedAsync(requestId, cancellationToken);
        var visitor = request.Visitor(visitorId);
        var document = visitor.CurrentDpsDocument;
        if (document is null || document.Id != documentId)
        {
            throw new NotFoundException("The current DPS document was not found.");
        }

        var download = await documentStorage.OpenPdfAsync(document.BlobName, document.FileName, document.Size, cancellationToken);
        try
        {
            var now = clock.UtcNow;
            await repository.AddDocumentAccessAsync(new DocumentAccessEvent(document.Id, request.Id, visitor.Id, actor.ObjectId, "Download", actor.CorrelationId, now), cancellationToken);
            request.RecordAudit("DpsDownloaded", actor.ObjectId, actor.Role.ToString(), null, new { document.Id, document.FileName, visitor.Id }, $"DPS version {document.Version} downloaded.", actor.CorrelationId, now);
            await repository.SaveChangesAsync(cancellationToken);
            return download;
        }
        catch
        {
            await download.Content.DisposeAsync();
            throw;
        }
    }

    private async Task<RequestDetailResponse> ApplyReceptionActionAsync(
        Guid requestId,
        byte[] rowVersion,
        string action,
        Action<VisitorRequest> operation,
        CancellationToken cancellationToken)
    {
        EnsureRole(UserRole.Reception);
        RequestValidator.ValidateRowVersion(rowVersion);
        var request = await GetAuthorizedAsync(requestId, cancellationToken);
        repository.SetExpectedRowVersion(request, rowVersion);
        var before = ResponseMapper.ToAuditSnapshot(request);
        operation(request);
        var now = clock.UtcNow;
        request.RecordAudit(action, actor.ObjectId, actor.Role.ToString(), before, ResponseMapper.ToAuditSnapshot(request), action, actor.CorrelationId, now);
        await repository.SaveChangesAsync(cancellationToken);
        return ResponseMapper.ToDetail(request, false);
    }

    private RequestQuery BuildRoleQuery(string? search, VisitorRequestStatus? status, string? siteCode, DateOnly? visitDate, int page, int pageSize)
    {
        if (actor.Role == UserRole.HostRequester)
        {
            return new RequestQuery(search, status, siteCode, visitDate, actor.ObjectId, page, pageSize);
        }

        if (actor.Role == UserRole.Reception)
        {
            var allowed = new[] { VisitorRequestStatus.Approved, VisitorRequestStatus.PartiallyApproved, VisitorRequestStatus.VisitCompleted };
            return new RequestQuery(search, status, siteCode, visitDate, null, page, pageSize, allowed);
        }

        return new RequestQuery(search, status, siteCode, visitDate, null, page, pageSize);
    }

    private async Task<VisitorRequest> GetHostRequestAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var request = await GetRequiredAsync(requestId, cancellationToken);
        if (!string.Equals(request.RequesterObjectId, actor.ObjectId, StringComparison.Ordinal))
        {
            throw new ForbiddenException("Only the request owner can change this request.");
        }

        return request;
    }

    private async Task<VisitorRequest> GetAuthorizedAsync(Guid requestId, CancellationToken cancellationToken)
    {
        EnsureAuthenticated();
        var request = await GetRequiredAsync(requestId, cancellationToken);
        var allowed = actor.Role switch
        {
            UserRole.HostRequester => string.Equals(request.RequesterObjectId, actor.ObjectId, StringComparison.Ordinal),
            UserRole.ExportControl => request.VisitorType == VisitorType.External,
            UserRole.Reception => request.Status is VisitorRequestStatus.Approved or VisitorRequestStatus.PartiallyApproved or VisitorRequestStatus.VisitCompleted,
            _ => false
        };
        if (!allowed)
        {
            throw new ForbiddenException("You do not have access to this request.");
        }

        return request;
    }

    private async Task<VisitorRequest> GetRequiredAsync(Guid requestId, CancellationToken cancellationToken) =>
        await repository.GetAsync(requestId, cancellationToken) ?? throw new NotFoundException("The visitor request was not found.");

    private async Task AddNotificationAsync(string eventName, VisitorRequest request, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new
        {
            EventName = eventName,
            request.Id,
            request.RequestNumber,
            request.RequesterObjectId,
            Status = request.Status.ToString(),
            OccurredAt = clock.UtcNow
        });
        await repository.AddOutboxAsync(new OutboxMessage(eventName, payload, clock.UtcNow), cancellationToken);
    }

    private void EnsureRole(UserRole expected)
    {
        EnsureAuthenticated();
        if (actor.Role != expected)
        {
            throw new ForbiddenException($"This action requires the {expected} role.");
        }
    }

    private void EnsureAuthenticated()
    {
        if (!actor.IsAuthenticated || string.IsNullOrWhiteSpace(actor.ObjectId))
        {
            throw new ForbiddenException("Authentication is required.");
        }
    }

    private static void ValidateFilters(string? search, string? siteCode)
    {
        var errors = new Dictionary<string, string[]>();
        if (search?.Length > 200) errors["search"] = ["Search cannot exceed 200 characters."];
        if (siteCode?.Length > 50) errors["siteCode"] = ["Site cannot exceed 50 characters."];
        if (errors.Count > 0) throw new ValidationException(errors);
    }
}
