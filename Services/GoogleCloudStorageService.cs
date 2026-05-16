using System.Net;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Options;
using SWP_BE.Options;

namespace SWP_BE.Services;

public sealed class GoogleCloudStorageService(
    StorageClient storageClient,
    IHttpClientFactory httpClientFactory,
    IOptions<StorageOptions> options) : IFileStorageService
{
    private readonly StorageOptions storageOptions = options.Value;

    public async Task<StoredFileResult> UploadAsync(
        Stream content,
        string objectName,
        string contentType,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();

        var uploaded = await storageClient.UploadObjectAsync(
            storageOptions.BucketName,
            objectName,
            contentType,
            content,
            cancellationToken: cancellationToken);

        return new StoredFileResult(
            uploaded.Name,
            uploaded.ContentType ?? contentType,
            (long?)uploaded.Size ?? 0,
            DateTimeOffset.UtcNow);
    }

    public async Task<StoredFileResult> ImportFromUrlAsync(
        Uri sourceUrl,
        string objectName,
        IReadOnlySet<string> allowedContentTypes,
        long maxBytes,
        CancellationToken cancellationToken)
    {
        EnsureSafeImportUrl(sourceUrl);

        var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, sourceUrl);
        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        if (!allowedContentTypes.Contains(contentType))
        {
            throw new InvalidOperationException($"Unsupported content type: {contentType}.");
        }

        var contentLength = response.Content.Headers.ContentLength;
        if (contentLength is > 0 && contentLength > maxBytes)
        {
            throw new InvalidOperationException($"File is too large. Max size is {maxBytes} bytes.");
        }

        await using var remoteStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var limitedStream = new MaxBytesReadStream(remoteStream, maxBytes);

        return await UploadAsync(limitedStream, objectName, contentType, cancellationToken);
    }

    public async Task<DownloadedFileResult> DownloadAsync(
        string objectName,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();

        var memoryStream = new MemoryStream();
        var downloaded = await storageClient.DownloadObjectAsync(
            storageOptions.BucketName,
            objectName,
            memoryStream,
            cancellationToken: cancellationToken);

        memoryStream.Position = 0;

        return new DownloadedFileResult(
            memoryStream,
            downloaded.ContentType ?? "application/octet-stream",
            (long?)downloaded.Size);
    }

    public async Task<string> CreateSignedReadUrlAsync(
        string objectName,
        TimeSpan? duration,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();

        var credential = await GoogleCredential.GetApplicationDefaultAsync(cancellationToken);
        var signer = UrlSigner.FromCredential(credential);
        var expiresIn = duration ?? TimeSpan.FromMinutes(storageOptions.SignedUrlMinutes);

        return await signer.SignAsync(
            storageOptions.BucketName,
            objectName,
            expiresIn,
            HttpMethod.Get,
            cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(string objectName, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        try
        {
            await storageClient.DeleteObjectAsync(
                storageOptions.BucketName,
                objectName,
                cancellationToken: cancellationToken);
        }
        catch (GoogleApiException exception) when (exception.HttpStatusCode == HttpStatusCode.NotFound)
        {
        }
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(storageOptions.BucketName))
        {
            throw new InvalidOperationException("Storage bucket is not configured.");
        }
    }

    private static void EnsureSafeImportUrl(Uri sourceUrl)
    {
        if (!sourceUrl.IsAbsoluteUri || sourceUrl.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("Only absolute HTTPS URLs are allowed.");
        }

        if (sourceUrl.IsLoopback)
        {
            throw new InvalidOperationException("Loopback URLs are not allowed.");
        }

        var addresses = Dns.GetHostAddresses(sourceUrl.Host);
        if (addresses.Length == 0 || addresses.Any(IsPrivateAddress))
        {
            throw new InvalidOperationException("Private network URLs are not allowed.");
        }
    }

    private static bool IsPrivateAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return bytes[0] == 10
                || bytes[0] == 127
                || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                || (bytes[0] == 192 && bytes[1] == 168)
                || (bytes[0] == 169 && bytes[1] == 254);
        }

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            return address.IsIPv6LinkLocal
                || address.IsIPv6SiteLocal
                || address.Equals(IPAddress.IPv6Loopback)
                || address.GetAddressBytes()[0] == 0xfc
                || address.GetAddressBytes()[0] == 0xfd;
        }

        return true;
    }

    private sealed class MaxBytesReadStream(Stream inner, long maxBytes) : Stream
    {
        private long bytesRead;

        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => bytesRead;
            set => throw new NotSupportedException();
        }

        public override void Flush() => inner.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = inner.Read(buffer, offset, count);
            CountBytes(read);
            return read;
        }

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            var read = await inner.ReadAsync(buffer, cancellationToken);
            CountBytes(read);
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        private void CountBytes(int read)
        {
            bytesRead += read;
            if (bytesRead > maxBytes)
            {
                throw new InvalidOperationException($"File is too large. Max size is {maxBytes} bytes.");
            }
        }
    }
}
