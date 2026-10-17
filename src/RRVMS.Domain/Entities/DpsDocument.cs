using RRVMS.Domain.Common;

namespace RRVMS.Domain.Entities;

public sealed class DpsDocument : Entity
{
    private DpsDocument()
    {
    }

    internal DpsDocument(Guid visitorId, string blobName, string fileName, long size, string sha256, string uploadedBy, DateTimeOffset uploadedAt, int version)
    {
        VisitorId = visitorId;
        BlobName = blobName;
        FileName = fileName;
        Size = size;
        Sha256 = sha256;
        UploadedBy = uploadedBy;
        UploadedAt = uploadedAt;
        Version = version;
    }

    public Guid VisitorId { get; private set; }
    public string BlobName { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public long Size { get; private set; }
    public string Sha256 { get; private set; } = string.Empty;
    public string UploadedBy { get; private set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; private set; }
    public int Version { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    internal void MarkDeleted(DateTimeOffset deletedAt) => DeletedAt = deletedAt;
}

