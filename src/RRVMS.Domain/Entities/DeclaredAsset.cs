using RRVMS.Domain.Common;
using RRVMS.Domain.Enums;

namespace RRVMS.Domain.Entities;

public sealed class DeclaredAsset : Entity
{
    private DeclaredAsset()
    {
    }

    internal DeclaredAsset(Guid visitorId, string assetType, string description, string serialNumber)
    {
        VisitorId = visitorId;
        AssetType = assetType.Trim();
        Description = description.Trim();
        SerialNumber = serialNumber.Trim();
        VerificationStatus = VerificationStatus.Pending;
    }

    public Guid VisitorId { get; private set; }
    public string AssetType { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string SerialNumber { get; private set; } = string.Empty;
    public VerificationStatus VerificationStatus { get; private set; }

    internal void MarkVerified() => VerificationStatus = VerificationStatus.Approved;
    internal void MarkRejected() => VerificationStatus = VerificationStatus.Rejected;
    internal void Reset() => VerificationStatus = VerificationStatus.Pending;
}

