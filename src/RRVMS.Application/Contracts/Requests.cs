using RRVMS.Domain.Enums;

namespace RRVMS.Application.Contracts;

public sealed record CreateVisitorRequestCommand(
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
    int NumberOfVisitors,
    DateTimeOffset VisitStart,
    DateTimeOffset VisitEnd);

public sealed record AssetInput(string AssetType, string Description, string SerialNumber);

public sealed record SubmitVisitorCommand(
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
    IReadOnlyCollection<AssetInput> Assets,
    byte[] RowVersion);

public sealed record SubmitForScreeningCommand(byte[] RowVersion);
public sealed record ReviseRequestCommand(
    string SiteCode,
    VisitPurposeType PurposeType,
    string Purpose,
    string AreasToVisit,
    string MainHostObjectId,
    string MainHostName,
    string HostDepartment,
    string EscortingHostObjectId,
    string EscortingHostName,
    byte[] RowVersion);
public sealed record ReviewVisitorsCommand(IReadOnlyCollection<Guid> VisitorIds, ScreeningDecision Decision, VisitorClassification Classification, string Comments, byte[] RowVersion);
public sealed record RequestCorrectionCommand(Guid VisitorId, IReadOnlyCollection<string> Fields, string Instructions, byte[] RowVersion);
public sealed record RescheduleCommand(DateTimeOffset VisitStart, DateTimeOffset VisitEnd, string Reason, byte[] RowVersion);
public sealed record CancelRequestCommand(string Reason, byte[] RowVersion);
public sealed record VerifyEntryCommand(Guid VisitorId, Guid VisitDayId, bool Approved, bool IdentityConfirmed, bool AssetsConfirmed, string Remarks, byte[] RowVersion);
public sealed record CheckInCommand(Guid VisitorId, Guid VisitDayId, string BadgeId, byte[] RowVersion);
public sealed record CheckOutCommand(Guid VisitorId, Guid VisitDayId, byte[] RowVersion);
public sealed record MarkNoShowCommand(Guid VisitorId, Guid VisitDayId, byte[] RowVersion);
