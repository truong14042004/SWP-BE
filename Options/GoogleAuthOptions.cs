namespace SWP_BE.Options;

public sealed class GoogleAuthOptions
{
    public const string SectionName = "Authentication:Google";

    public string ClientId { get; init; } = string.Empty;
}
