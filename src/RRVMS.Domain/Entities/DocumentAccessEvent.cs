using RRVMS.Domain.Common;

namespace RRVMS.Domain.Entities;

public sealed class DocumentAccessEvent : Entity
{
    private DocumentAccessEvent()
    {
    }

    public DocumentAccessEvent(Guid documentId, Guid requestId, Guid visitorId, string actorId, string action, string correlationId, DateTimeOffset occurredAt)
    {
        DocumentId = documentId;
        RequestId = requestId;
        VisitorId = visitorId;
        ActorId = actorId;
        Action = action;
        CorrelationId = correlationId;
        OccurredAt = occurredAt;
    }

    public Guid DocumentId { get; private set; }
    public Guid RequestId { get; private set; }
    public Guid VisitorId { get; private set; }
    public string ActorId { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }
}
