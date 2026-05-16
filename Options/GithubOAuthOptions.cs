namespace SWP_BE.Options;

public sealed class GithubOAuthOptions
{
    public const string SectionName = "GitHubOAuth";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
}
