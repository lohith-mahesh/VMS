using RRVMS.Domain.Common;

namespace RRVMS.Domain.Entities;

public sealed class AuditEvent : Entity
{
    private AuditEvent()
    {
    }

    internal AuditEvent(Guid requestId, string action, string actorId, string actorRole, string beforeJson, string afterJson, string details, string correlationId, DateTimeOffset occurredAt)
    {
        RequestId = requestId;
        Action = action;
        ActorId = actorId;
        ActorRole = actorRole;
        BeforeJson = beforeJson;
        AfterJson = afterJson;
        Details = details;
        CorrelationId = correlationId;
        OccurredAt = occurredAt;
    }

    public Guid RequestId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string ActorId { get; private set; } = string.Empty;
    public string ActorRole { get; private set; } = string.Empty;
    public string BeforeJson { get; private set; } = string.Empty;
    public string AfterJson { get; private set; } = string.Empty;
    public string Details { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }
}

