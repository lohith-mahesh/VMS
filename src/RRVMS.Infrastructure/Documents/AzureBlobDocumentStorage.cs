using System.Security.Cryptography;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using RRVMS.Application.Abstractions;
using RRVMS.Application.Common;

namespace RRVMS.Infrastructure.Documents;

public sealed class AzureBlobDocumentStorage(BlobContainerClient container) : IDocumentStorage
{
    public async Task<StoredDocument> SavePdfAsync(Guid requestId, Guid visitorId, int version, Stream content, string fileName, CancellationToken cancellationToken)
    {
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
        var blobName = $"{requestId:N}/{visitorId:N}/{version:D4}-{Guid.NewGuid():N}.pdf";
        await using var buffered = new MemoryStream();
        await content.CopyToAsync(buffered, cancellationToken);
        var hash = Convert.ToHexString(SHA256.HashData(buffered.GetBuffer().AsSpan(0, (int)buffered.Length)));
        buffered.Position = 0;
        var headers = new BlobHttpHeaders
        {
            ContentType = "application/pdf",
            ContentDisposition = $"attachment; filename=\"{SanitizeHeaderValue(fileName)}\""
        };
        await container.GetBlobClient(blobName).UploadAsync(buffered, new BlobUploadOptions { HttpHeaders = headers }, cancellationToken);
        return new StoredDocument(blobName, hash, buffered.Length);
    }

    public async Task<DocumentDownload> OpenPdfAsync(string blobName, string fileName, long size, CancellationToken cancellationToken)
    {
        var blob = container.GetBlobClient(blobName);
        if (!await blob.ExistsAsync(cancellationToken))
        {
            throw new NotFoundException("The DPS document content was not found.");
        }

        var response = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return new DocumentDownload(response.Value.Content, fileName, "application/pdf", size);
    }

    public async Task DeleteAsync(string blobName, CancellationToken cancellationToken) =>
        await container.GetBlobClient(blobName).DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);

    private static string SanitizeHeaderValue(string value) => value.Replace("\r", string.Empty).Replace("\n", string.Empty).Replace("\"", string.Empty);
}
