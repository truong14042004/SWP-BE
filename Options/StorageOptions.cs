namespace SWP_BE.Options;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string Provider { get; set; } = "GoogleCloudStorage";

    public string? ProjectId { get; set; }

    public string BucketName { get; set; } = string.Empty;

    public int SignedUrlMinutes { get; set; } = 30;

    public long MaxUploadBytes { get; set; } = 10 * 1024 * 1024;
}
