namespace SWP_BE.Options;

public sealed class GithubOptions
{
    public const string SectionName = "GitHub";

    public string Token { get; set; } = string.Empty;
}
