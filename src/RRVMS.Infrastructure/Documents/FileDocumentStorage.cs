using System.Security.Cryptography;
using RRVMS.Application.Abstractions;
using RRVMS.Application.Common;
using RRVMS.Infrastructure.Options;

namespace RRVMS.Infrastructure.Documents;

public sealed class FileDocumentStorage(StorageOptions options) : IDocumentStorage
{
    private readonly string _root = Path.GetFullPath(options.LocalPath);

    public async Task<StoredDocument> SavePdfAsync(Guid requestId, Guid visitorId, int version, Stream content, string fileName, CancellationToken cancellationToken)
    {
        var blobName = $"{requestId:N}/{visitorId:N}/{version:D4}-{Guid.NewGuid():N}.pdf";
        var path = Resolve(blobName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var destination = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.WriteThrough);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[81920];
        long size = 0;
        int read;
        while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            hash.AppendData(buffer, 0, read);
            size += read;
        }

        await destination.FlushAsync(cancellationToken);
        return new StoredDocument(blobName, Convert.ToHexString(hash.GetHashAndReset()), size);
    }

    public Task<DocumentDownload> OpenPdfAsync(string blobName, string fileName, long size, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = Resolve(blobName);
        if (!File.Exists(path))
        {
            throw new NotFoundException("The DPS document content was not found.");
        }

        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Task.FromResult(new DocumentDownload(stream, fileName, "application/pdf", size));
    }

    public Task DeleteAsync(string blobName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = Resolve(blobName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private string Resolve(string blobName)
    {
        var normalized = blobName.Replace('/', Path.DirectorySeparatorChar);
        var path = Path.GetFullPath(Path.Combine(_root, normalized));
        if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The document path is invalid.");
        }

        return path;
    }
}
