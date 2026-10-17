namespace RRVMS.Domain.Enums;

public enum VisitorRequestStatus
{
    Draft = 1,
    VisitorFormPending = 2,
    VisitorFormSubmitted = 3,
    HostReview = 4,
    PendingEcReview = 5,
    PendingDocumentation = 6,
    DocumentationSubmitted = 7,
    EcReReviewRequired = 8,
    Approved = 9,
    PartiallyApproved = 10,
    Rejected = 11,
    Cancelled = 12,
    VisitProcessCompleted = 13
}

