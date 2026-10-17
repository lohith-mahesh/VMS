using RRVMS.Domain.Enums;

namespace RRVMS.Application.Contracts;

public sealed record MeResponse(string ObjectId, string DisplayName, string Email, UserRole Role);

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);

public sealed record RequestSummaryResponse(
    Guid Id,
    string RequestNumber,
    VisitorRequestStatus Status,
    VisitorType VisitorType,
    string SiteCode,
    string MainHostName,
    string HostDepartment,
    DateTimeOffset VisitStart,
    DateTimeOffset VisitEnd,
    int VisitorCount,
    int SubmittedVisitorCount,
    DateTimeOffset UpdatedAt,
    byte[] RowVersion,
    IReadOnlyList<string> VisitorNames,
    IReadOnlyList<string> CompanyNames);

public sealed record AssetResponse(Guid Id, string AssetType, string Description, string SerialNumber, VerificationStatus VerificationStatus);
public sealed record DpsDocumentResponse(Guid Id, string FileName, long Size, int Version, DateTimeOffset UploadedAt, string UploadedBy);
public sealed record ReceptionRecordResponse(
    Guid Id,
    Guid VisitDayId,
    ReceptionStatus Status,
    VerificationStatus IdentityStatus,
    VerificationStatus AssetsStatus,
    ArrivalStatus ArrivalStatus,
    string BadgeId,
    string BadgeType,
    string VerificationRemarks,
    DateTimeOffset? ActualArrival,
    DateTimeOffset? ActualDeparture);

public sealed record VisitorResponse(
    Guid Id,
    int Sequence,
    VisitorDetailsStatus DetailsStatus,
    string FullName,
    string FirstName,
    string MiddleName,
    string LastName,
    string Citizenship,
    string Designation,
    string CompanyName,
    string CompanyAddress,
    string OfficeCity,
    string OfficeCountry,
    string PhoneCountry,
    string PhoneDialCode,
    string Telephone,
    string Email,
    string IdType,
    string OtherIdType,
    ScreeningDecision ScreeningDecision,
    VisitorClassification Classification,
    string ScreeningReason,
    string BadgeType,
    IReadOnlyList<AssetResponse> Assets,
    DpsDocumentResponse? DpsDocument,
    IReadOnlyList<ReceptionRecordResponse> ReceptionRecords);

public sealed record VisitDayResponse(Guid Id, DateOnly Date, TimeOnly ExpectedArrival, TimeOnly ExpectedDeparture);
public sealed record AuditEventResponse(Guid Id, string Action, string ActorId, string ActorRole, string BeforeJson, string AfterJson, string Details, DateTimeOffset OccurredAt);
public sealed record InformationRequestResponse(Guid Id, Guid VisitorId, string FieldsJson, string Instructions, string ChangesJson, InformationRequestStatus Status, DateTimeOffset CreatedAt, DateTimeOffset? ResolvedAt);
public sealed record ScreeningReviewResponse(Guid Id, Guid VisitorId, ScreeningDecision Decision, VisitorClassification Classification, string Comments, string ReviewerId, DateTimeOffset ReviewedAt);
public sealed record ScheduleChangeResponse(Guid Id, DateTimeOffset PreviousStart, DateTimeOffset PreviousEnd, DateTimeOffset NewStart, DateTimeOffset NewEnd, string Reason, string ChangedBy, DateTimeOffset ChangedAt);

public sealed record RequestDetailResponse(
    Guid Id,
    string RequestNumber,
    string RequesterObjectId,
    VisitorRequestStatus Status,
    VisitorType VisitorType,
    ContractorType ContractorType,
    string SiteCode,
    VisitPurposeType PurposeType,
    string Purpose,
    string AreasToVisit,
    string MainHostObjectId,
    string MainHostName,
    string HostDepartment,
    string EscortingHostObjectId,
    string EscortingHostName,
    DateTimeOffset VisitStart,
    DateTimeOffset VisitEnd,
    string CancellationReason,
    IReadOnlyList<VisitorResponse> Visitors,
    IReadOnlyList<VisitDayResponse> VisitDays,
    IReadOnlyList<AuditEventResponse> AuditEvents,
    IReadOnlyList<InformationRequestResponse> InformationRequests,
    IReadOnlyList<ScreeningReviewResponse> ScreeningReviews,
    IReadOnlyList<ScheduleChangeResponse> ScheduleChanges,
    byte[] RowVersion);

public sealed record DashboardResponse(
    int TotalVisitors,
    int PendingActions,
    int TodayVisitors,
    int CheckedIn,
    int Upcoming,
    int NoShows,
    int PendingEcReview,
    int PendingDocumentation,
    int Approved,
    int CheckedOut,
    IReadOnlyList<RequestSummaryResponse> RecentRequests,
    int Rejected,
    int ReceptionRejections,
    int VisitorHistory,
    IReadOnlyList<RequestSummaryResponse> PendingReviewRequests,
    IReadOnlyList<RequestSummaryResponse> PendingDocumentationRequests,
    IReadOnlyList<ReceptionIndexRowResponse> ReceptionItems);

public sealed record ReceptionIndexRowResponse(
    Guid RequestId,
    Guid VisitorId,
    Guid VisitDayId,
    string RequestNumber,
    string VisitorName,
    string CompanyName,
    string HostName,
    string EscortName,
    string PersonType,
    DateOnly VisitDate,
    bool ScheduledToday,
    ReceptionStatus Status,
    ScreeningDecision ScreeningDecision,
    VerificationStatus IdentityStatus,
    VerificationStatus AssetsStatus,
    string BadgeId,
    string BadgeType,
    string AssetSummary);

public sealed record ReportRowResponse(
    Guid RequestId,
    Guid VisitorId,
    DateOnly VisitDate,
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
    string AssetSummary,
    string BadgeType,
    string BadgeId,
    string VerificationRemarks);

public sealed record ReportPageResponse(
    IReadOnlyList<ReportRowResponse> Items,
    int Page,
    int PageSize,
    int Total,
    int MatchingVisitors,
    int MatchingRequests,
    int CheckedIn,
    int AssetsDeclared);
