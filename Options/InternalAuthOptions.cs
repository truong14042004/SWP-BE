namespace SWP_BE.Options;

public sealed class InternalAuthOptions
{
    public const string SectionName = "InternalAuth";

    public string Token { get; set; } = string.Empty;
}
