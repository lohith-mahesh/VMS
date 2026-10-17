using RRVMS.Domain.Common;

namespace RRVMS.Domain.Entities;

public sealed class ReportExportEvent : Entity
{
    private ReportExportEvent()
    {
    }

    public ReportExportEvent(string actorId, string actorRole, string format, string filtersJson, int rowCount, string correlationId, DateTimeOffset exportedAt)
    {
        ActorId = actorId;
        ActorRole = actorRole;
        Format = format;
        FiltersJson = filtersJson;
        RowCount = rowCount;
        CorrelationId = correlationId;
        ExportedAt = exportedAt;
    }

    public string ActorId { get; private set; } = string.Empty;
    public string ActorRole { get; private set; } = string.Empty;
    public string Format { get; private set; } = string.Empty;
    public string FiltersJson { get; private set; } = string.Empty;
    public int RowCount { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
    public DateTimeOffset ExportedAt { get; private set; }
}
