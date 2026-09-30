namespace RRVMS.Domain.Enums;

public enum VisitorRequestStatus
{
    Draft = 1,
    VisitorDetailsPending = 2,
    ReadyForScreening = 3,
    PendingScreening = 4,
    PendingCorrection = 5,
    CorrectionSubmitted = 6,
    Approved = 7,
    PartiallyApproved = 8,
    Rejected = 9,
    Cancelled = 10,
    VisitCompleted = 11
}

