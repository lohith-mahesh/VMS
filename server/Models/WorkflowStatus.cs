namespace RRVMS.Api.Models;

public static class WorkflowStatus
{
    public const string VisitorFormPending = nameof(RequestStatus.VISITOR_FORM_PENDING);
    public const string VisitorFormSubmitted = nameof(RequestStatus.VISITOR_FORM_SUBMITTED);
    public const string HostReview = nameof(RequestStatus.HOST_REVIEW);
    public const string HostFinalSubmitted = nameof(RequestStatus.HOST_FINAL_SUBMITTED);
    public const string HostDps = nameof(RequestStatus.HOST_DPS);
    public const string EcDps = nameof(RequestStatus.EC_DPS);
    public const string EcReview = nameof(RequestStatus.EC_REVIEW);
    public const string PendingDocumentation = nameof(RequestStatus.PENDING_DOCUMENTATION);
    public const string DocumentationSubmitted = nameof(RequestStatus.DOCUMENTATION_SUBMITTED);
    public const string EcReReviewRequired = nameof(RequestStatus.EC_RE_REVIEW_REQUIRED);
    public const string Approved = nameof(RequestStatus.APPROVED);
    public const string Rejected = nameof(RequestStatus.REJECTED);
    public const string ReceptionHold = nameof(RequestStatus.RECEPTION_HOLD);
    public const string Completed = nameof(RequestStatus.VISIT_PROCESS_COMPLETED);
    public const string CancelledPersonnelChange = nameof(RequestStatus.CANCELLED_PERSONNEL_CHANGE);
}