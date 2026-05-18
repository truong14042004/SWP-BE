namespace SWP_BE.Options;

public sealed class PayOsOptions
{
    public const string SectionName = "PayOS";

    public string ClientId { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string ChecksumKey { get; set; } = string.Empty;

    public string ReturnUrl { get; set; } = string.Empty;

    public string CancelUrl { get; set; } = string.Empty;

    public string WebhookUrl { get; set; } = string.Empty;
}
