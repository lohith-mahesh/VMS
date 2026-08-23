namespace RRVMS.Api.Models;

public static class WorkflowStatus
{
    public const string Submitted = "VISITOR_FORM_PENDING";
    public const string HostReview = "HOST_REVIEW";
    public const string PendingDps = "DPS_PENDING";
    public const string HostDps = "HOST_DPS";
    public const string EcDpsReview = "EC_DPS_REVIEW";
    public const string EcReview = "EC_REVIEW";
    public const string PendingDocumentation = "PENDING_DOCUMENTATION";
    public const string DocumentationSubmitted = "DOCUMENTATION_SUBMITTED";
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";
    public const string EcReReviewRequired = "EC_RE_REVIEW_REQUIRED";
    public const string SecurityVerification = "SECURITY_VERIFICATION";
    public const string SecurityHold = "SECURITY_HOLD_EC_REVIEW";
    public const string EntryRejected = "ENTRY_REJECTED";
    public const string CheckedIn = "CHECKED_IN";
    public const string CheckedOut = "CHECKED_OUT";
    public const string Completed = "VISIT_PROCESS_COMPLETED";
    public const string CancelledPersonnelChange = "CANCELLED_PERSONNEL_CHANGE";
}