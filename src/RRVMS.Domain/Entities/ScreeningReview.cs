using RRVMS.Domain.Common;
using RRVMS.Domain.Enums;

namespace RRVMS.Domain.Entities;

public sealed class ScreeningReview : Entity
{
    private ScreeningReview()
    {
    }

    internal ScreeningReview(Guid requestId, Guid visitorId, ScreeningDecision decision, VisitorClassification classification, string comments, string reviewerId, DateTimeOffset reviewedAt)
    {
        RequestId = requestId;
        VisitorId = visitorId;
        Decision = decision;
        Classification = classification;
        Comments = comments;
        ReviewerId = reviewerId;
        ReviewedAt = reviewedAt;
    }

    public Guid RequestId { get; private set; }
    public Guid VisitorId { get; private set; }
    public ScreeningDecision Decision { get; private set; }
    public VisitorClassification Classification { get; private set; }
    public string Comments { get; private set; } = string.Empty;
    public string ReviewerId { get; private set; } = string.Empty;
    public DateTimeOffset ReviewedAt { get; private set; }
}

