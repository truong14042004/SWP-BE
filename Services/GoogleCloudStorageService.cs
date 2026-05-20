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
        CancellationToken cancellationToken,
        string? downloadFileName = null)
    {
        EnsureConfigured();

        var expiresIn = duration ?? TimeSpan.FromMinutes(storageOptions.SignedUrlMinutes);

        // Build request template — same for both signing paths.
        // When a download file name is supplied, ask GCS to set Content-Disposition: attachment;
        // filename="..." so the browser saves the file with the original human name instead of
        // the storage object path (e.g. "roadmap-evidence/<userId>/<timestamp>-foo.zip").
        var requestTemplate = UrlSigner.RequestTemplate
            .FromBucket(storageOptions.BucketName)
            .WithObjectName(objectName)
            .WithHttpMethod(HttpMethod.Get);

        if (!string.IsNullOrWhiteSpace(downloadFileName))
        {
            var safeName = SanitizeContentDispositionFileName(downloadFileName);
            requestTemplate = requestTemplate.WithRequestHeaders(new Dictionary<string, IEnumerable<string>>
            {
                ["response-content-disposition"] = new[] { $"attachment; filename=\"{safeName}\"" }
            });
        }

        var optionsTemplate = UrlSigner.Options.FromDuration(expiresIn);

        var credential = await GoogleCredential.GetApplicationDefaultAsync(cancellationToken);

        // ServiceAccountCredential has a private key embedded → sign locally.
        if (credential.UnderlyingCredential is ServiceAccountCredential)
        {
            var localSigner = UrlSigner.FromCredential(credential);
            return await localSigner.SignAsync(requestTemplate, optionsTemplate, cancellationToken);
        }

        // Cloud Run / GCE / GKE: ComputeCredential has no private key locally.
        // Sign through the IAM Credentials API (requires roles/iam.serviceAccountTokenCreator on itself).
        var blobSigner = await BuildIamBlobSignerAsync(credential, cancellationToken);
        var signer = UrlSigner.FromBlobSigner(blobSigner);
        return await signer.SignAsync(requestTemplate, optionsTemplate, cancellationToken);
    }

    private static string SanitizeContentDispositionFileName(string name)
    {
        // Strip path separators and quote characters that would break the header.
        // Non-ASCII characters survive — Cloud Storage forwards them as-is and modern browsers
        // accept the unencoded form for Content-Disposition.
        return name
            .Replace('\\', '_')
            .Replace('/', '_')
            .Replace('"', '\'')
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty);
    }

    private async Task<UrlSigner.IBlobSigner> BuildIamBlobSignerAsync(
        GoogleCredential credential,
        CancellationToken cancellationToken)
    {
        // Resolve the service account email from metadata server (works on Cloud Run/GCE/GKE).
        var http = httpClientFactory.CreateClient();
        http.DefaultRequestHeaders.TryAddWithoutValidation("Metadata-Flavor", "Google");

        var saEmail = await http.GetStringAsync(
            "http://metadata.google.internal/computeMetadata/v1/instance/service-accounts/default/email",
            cancellationToken);
        saEmail = saEmail.Trim();

        if (string.IsNullOrWhiteSpace(saEmail))
        {
            throw new InvalidOperationException(
                "Could not resolve runtime service account email from metadata server.");
        }

        var scoped = credential.CreateScoped("https://www.googleapis.com/auth/iam");
        return new IamBlobSigner(scoped, saEmail, httpClientFactory);
    }

    private sealed class IamBlobSigner(
        GoogleCredential credential,
        string serviceAccountEmail,
        IHttpClientFactory httpClientFactory) : UrlSigner.IBlobSigner
    {
        public string Id => serviceAccountEmail;

        public string Algorithm => "GOOG4-RSA-SHA256";

        public async Task<string> CreateSignatureAsync(
            byte[] data,
            UrlSigner.BlobSignerParameters parameters,
            CancellationToken cancellationToken)
        {
            var token = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync(
                cancellationToken: cancellationToken);

            var http = httpClientFactory.CreateClient();
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var requestBody = new
            {
                payload = Convert.ToBase64String(data)
            };

            var response = await http.PostAsync(
                $"https://iamcredentials.googleapis.com/v1/projects/-/serviceAccounts/{Uri.EscapeDataString(serviceAccountEmail)}:signBlob",
                System.Net.Http.Json.JsonContent.Create(requestBody),
                cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"IAM signBlob failed with {(int)response.StatusCode}: {responseBody}");
            }

            using var doc = System.Text.Json.JsonDocument.Parse(responseBody);
            return doc.RootElement.GetProperty("signedBlob").GetString()
                ?? throw new InvalidOperationException("IAM signBlob response missing signedBlob.");
        }

        public string CreateSignature(byte[] data, UrlSigner.BlobSignerParameters parameters) =>
            CreateSignatureAsync(data, parameters, CancellationToken.None).GetAwaiter().GetResult();
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
