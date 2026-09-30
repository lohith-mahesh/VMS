using RRVMS.Application.Contracts;
using RRVMS.Domain.Entities;
using RRVMS.Domain.Enums;

namespace RRVMS.Application.Mapping;

public static class ResponseMapper
{
    public static RequestSummaryResponse ToSummary(VisitorRequest request) => new(
        request.Id,
        request.RequestNumber,
        request.Status,
        request.VisitorType,
        request.SiteCode,
        request.MainHostName,
        request.HostDepartment,
        request.VisitStart,
        request.VisitEnd,
        request.Visitors.Count,
        request.Visitors.Count(visitor => visitor.DetailsStatus == VisitorDetailsStatus.Submitted),
        request.UpdatedAt,
        request.RowVersion);

    public static RequestDetailResponse ToDetail(VisitorRequest request, bool includeDpsMetadata) => new(
        request.Id,
        request.RequestNumber,
        request.RequesterObjectId,
        request.Status,
        request.VisitorType,
        request.ContractorType,
        request.SiteCode,
        request.PurposeType,
        request.Purpose,
        request.AreasToVisit,
        request.MainHostObjectId,
        request.MainHostName,
        request.HostDepartment,
        request.EscortingHostObjectId,
        request.EscortingHostName,
        request.VisitStart,
        request.VisitEnd,
        request.CancellationReason,
        request.Visitors.OrderBy(visitor => visitor.Sequence).Select(visitor => ToVisitor(request, visitor, includeDpsMetadata)).ToArray(),
        request.VisitDays.OrderBy(day => day.Date).Select(day => new VisitDayResponse(day.Id, day.Date, day.ExpectedArrival, day.ExpectedDeparture)).ToArray(),
        includeDpsMetadata
            ? request.AuditEvents.OrderByDescending(audit => audit.OccurredAt).Select(audit => new AuditEventResponse(
                audit.Id,
                audit.Action,
                audit.ActorId,
                audit.ActorRole,
                audit.BeforeJson,
                audit.AfterJson,
                audit.Details,
                audit.OccurredAt)).ToArray()
            : [],
        includeDpsMetadata
            ? request.InformationRequests.OrderByDescending(item => item.CreatedAt).Select(item => new InformationRequestResponse(
                item.Id,
                item.VisitorId,
                item.FieldsJson,
                item.Instructions,
                item.ChangesJson,
                item.Status,
                item.CreatedAt,
                item.ResolvedAt)).ToArray()
            : [],
        request.ScheduleChanges.OrderByDescending(change => change.ChangedAt).Select(change => new ScheduleChangeResponse(
            change.Id,
            change.PreviousStart,
            change.PreviousEnd,
            change.NewStart,
            change.NewEnd,
            change.Reason,
            change.ChangedBy,
            change.ChangedAt)).ToArray(),
        request.RowVersion);

    public static object ToAuditSnapshot(VisitorRequest request) => new
    {
        request.RequestNumber,
        request.Status,
        request.VisitorType,
        request.ContractorType,
        request.SiteCode,
        request.PurposeType,
        request.Purpose,
        request.AreasToVisit,
        request.MainHostObjectId,
        request.MainHostName,
        request.HostDepartment,
        request.EscortingHostObjectId,
        request.EscortingHostName,
        request.VisitStart,
        request.VisitEnd,
        request.CancellationReason,
        request.SubmittedAt,
        request.ApprovedAt,
        request.RejectedAt,
        request.RetainUntil,
        VisitDays = request.VisitDays.OrderBy(day => day.Date).Select(day => new
        {
            day.Id,
            day.Date,
            day.ExpectedArrival,
            day.ExpectedDeparture
        }).ToArray(),
        Visitors = request.Visitors.OrderBy(visitor => visitor.Sequence).Select(visitor => new
        {
            visitor.Id,
            visitor.Sequence,
            visitor.DetailsStatus,
            visitor.FirstName,
            visitor.MiddleName,
            visitor.LastName,
            visitor.FullName,
            visitor.Citizenship,
            visitor.Designation,
            visitor.CompanyName,
            visitor.CompanyAddress,
            visitor.OfficeCity,
            visitor.OfficeCountry,
            visitor.PhoneCountry,
            visitor.PhoneDialCode,
            visitor.Telephone,
            visitor.Email,
            visitor.IdType,
            visitor.OtherIdType,
            visitor.ScreeningDecision,
            visitor.Classification,
            visitor.ScreeningReason,
            visitor.ScreeningDecidedAt,
            DpsDocuments = visitor.DpsDocuments.OrderBy(document => document.Version).Select(document => new
            {
                document.Id,
                document.FileName,
                document.Size,
                document.Sha256,
                document.Version,
                document.UploadedBy,
                document.UploadedAt,
                document.DeletedAt
            }).ToArray(),
            Assets = visitor.Assets.OrderBy(asset => asset.Id).Select(asset => new
            {
                asset.Id,
                asset.AssetType,
                asset.Description,
                asset.SerialNumber,
                asset.VerificationStatus
            }).ToArray(),
            Reception = visitor.ReceptionRecords.OrderBy(record => record.VisitDayId).Select(record => new
            {
                record.Id,
                record.VisitDayId,
                record.Status,
                record.IdentityStatus,
                record.AssetsStatus,
                record.ArrivalStatus,
                record.BadgeId,
                record.BadgeType,
                record.VerificationRemarks,
                record.ActualArrival,
                record.ActualDeparture,
                record.BadgeReturnedAt
            }).ToArray()
        }).ToArray()
    };

    private static VisitorResponse ToVisitor(VisitorRequest request, Visitor visitor, bool includeDpsMetadata)
    {
        var currentDocument = includeDpsMetadata ? visitor.CurrentDpsDocument : null;
        return new VisitorResponse(
            visitor.Id,
            visitor.Sequence,
            visitor.DetailsStatus,
            visitor.FullName,
            visitor.FirstName,
            visitor.MiddleName,
            visitor.LastName,
            visitor.Citizenship,
            visitor.Designation,
            visitor.CompanyName,
            visitor.CompanyAddress,
            visitor.OfficeCity,
            visitor.OfficeCountry,
            visitor.PhoneCountry,
            visitor.PhoneDialCode,
            visitor.Telephone,
            visitor.Email,
            visitor.IdType,
            visitor.OtherIdType,
            visitor.ScreeningDecision,
            visitor.Classification,
            visitor.ScreeningReason,
            request.BadgeType(visitor),
            visitor.Assets.OrderBy(asset => asset.Id).Select(asset => new AssetResponse(
                asset.Id,
                asset.AssetType,
                asset.Description,
                asset.SerialNumber,
                asset.VerificationStatus)).ToArray(),
            currentDocument is null
                ? null
                : new DpsDocumentResponse(
                    currentDocument.Id,
                    currentDocument.FileName,
                    currentDocument.Size,
                    currentDocument.Version,
                    currentDocument.UploadedAt,
                    currentDocument.UploadedBy),
            visitor.ReceptionRecords.OrderBy(record => record.VisitDayId).Select(record => new ReceptionRecordResponse(
                record.Id,
                record.VisitDayId,
                record.Status,
                record.IdentityStatus,
                record.AssetsStatus,
                record.ArrivalStatus,
                record.BadgeId,
                record.BadgeType,
                record.VerificationRemarks,
                record.ActualArrival,
                record.ActualDeparture)).ToArray());
    }
}
