using RRVMS.Domain.Common;

namespace RRVMS.Domain.Entities;

public sealed class OutboxMessage : Entity
{
    private OutboxMessage()
    {
    }

    public OutboxMessage(string messageType, string payloadJson, DateTimeOffset occurredAt)
    {
        MessageType = messageType;
        PayloadJson = payloadJson;
        OccurredAt = occurredAt;
    }

    public string MessageType { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public int AttemptCount { get; private set; }
    public string LastError { get; private set; } = string.Empty;

    public void MarkProcessed(DateTimeOffset processedAt) => ProcessedAt = processedAt;

    public void MarkFailed(string error)
    {
        AttemptCount++;
        LastError = error;
    }
}
