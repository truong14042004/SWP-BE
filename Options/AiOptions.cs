namespace SWP_BE.Options;

public sealed class AiOptions
{
    public const string SectionName = "AI";

    public string Provider { get; set; } = "Gemini";

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gemini-1.5-flash";

    public string ProjectId { get; set; } = string.Empty;

    public string Location { get; set; } = "us-central1";

    public bool UseApplicationDefaultCredentials { get; set; } = true;
}
