using RRVMS.Domain.Common;
using RRVMS.Domain.Enums;

namespace RRVMS.Domain.Entities;

public sealed class InformationRequest : Entity
{
    private InformationRequest()
    {
    }

    internal InformationRequest(Guid requestId, Guid visitorId, string fieldsJson, string instructions, string originalValuesJson, string createdBy, DateTimeOffset createdAt)
    {
        RequestId = requestId;
        VisitorId = visitorId;
        FieldsJson = fieldsJson;
        Instructions = instructions;
        OriginalValuesJson = originalValuesJson;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
        Status = InformationRequestStatus.Pending;
    }

    public Guid RequestId { get; private set; }
    public Guid VisitorId { get; private set; }
    public string FieldsJson { get; private set; } = string.Empty;
    public string Instructions { get; private set; } = string.Empty;
    public string OriginalValuesJson { get; private set; } = string.Empty;
    public string ChangesJson { get; private set; } = "[]";
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public InformationRequestStatus Status { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }

    internal void Resolve(string changesJson, DateTimeOffset resolvedAt)
    {
        ChangesJson = changesJson;
        ResolvedAt = resolvedAt;
        Status = InformationRequestStatus.Resolved;
    }
}

