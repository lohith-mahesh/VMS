using RRVMS.Domain.Common;

namespace RRVMS.Domain.Entities;

public sealed class VisitorVersion : Entity
{
    private VisitorVersion()
    {
    }

    internal VisitorVersion(Guid visitorId, int version, string snapshotJson, string createdBy, DateTimeOffset createdAt)
    {
        VisitorId = visitorId;
        Version = version;
        SnapshotJson = snapshotJson;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
    }

    public Guid VisitorId { get; private set; }
    public int Version { get; private set; }
    public string SnapshotJson { get; private set; } = string.Empty;
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
}

