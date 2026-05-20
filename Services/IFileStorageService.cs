namespace SWP_BE.Services;

public interface IFileStorageService
{
    Task<StoredFileResult> UploadAsync(
        Stream content,
        string objectName,
        string contentType,
        CancellationToken cancellationToken);

    Task<StoredFileResult> ImportFromUrlAsync(
        Uri sourceUrl,
        string objectName,
        IReadOnlySet<string> allowedContentTypes,
        long maxBytes,
        CancellationToken cancellationToken);

    Task<DownloadedFileResult> DownloadAsync(
        string objectName,
        CancellationToken cancellationToken);

    Task<string> CreateSignedReadUrlAsync(
        string objectName,
        TimeSpan? duration,
        CancellationToken cancellationToken,
        string? downloadFileName = null);

    Task DeleteAsync(string objectName, CancellationToken cancellationToken);
}

public sealed record StoredFileResult(
    string ObjectName,
    string ContentType,
    long Size,
    DateTimeOffset UploadedAt);

public sealed record DownloadedFileResult(
    Stream Content,
    string ContentType,
    long? Size);
