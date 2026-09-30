using RRVMS.Domain.Common;

namespace RRVMS.Domain.Entities;

public sealed class ScheduleChange : Entity
{
    private ScheduleChange()
    {
    }

    internal ScheduleChange(Guid requestId, DateTimeOffset previousStart, DateTimeOffset previousEnd, DateTimeOffset newStart, DateTimeOffset newEnd, string reason, string changedBy, DateTimeOffset changedAt)
    {
        RequestId = requestId;
        PreviousStart = previousStart;
        PreviousEnd = previousEnd;
        NewStart = newStart;
        NewEnd = newEnd;
        Reason = reason;
        ChangedBy = changedBy;
        ChangedAt = changedAt;
    }

    public Guid RequestId { get; private set; }
    public DateTimeOffset PreviousStart { get; private set; }
    public DateTimeOffset PreviousEnd { get; private set; }
    public DateTimeOffset NewStart { get; private set; }
    public DateTimeOffset NewEnd { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string ChangedBy { get; private set; } = string.Empty;
    public DateTimeOffset ChangedAt { get; private set; }
}

