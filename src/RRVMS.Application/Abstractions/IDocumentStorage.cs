namespace RRVMS.Application.Abstractions;

public sealed record StoredDocument(string BlobName, string Sha256, long Size);
public sealed record DocumentDownload(Stream Content, string FileName, string ContentType, long Size);

public interface IDocumentStorage
{
    Task<StoredDocument> SavePdfAsync(Guid requestId, Guid visitorId, int version, Stream content, string fileName, CancellationToken cancellationToken);
    Task<DocumentDownload> OpenPdfAsync(string blobName, string fileName, long size, CancellationToken cancellationToken);
    Task DeleteAsync(string blobName, CancellationToken cancellationToken);
}

