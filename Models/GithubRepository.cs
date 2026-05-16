namespace SWP_BE.Models;

public sealed class GithubRepository
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string RepoName { get; set; } = string.Empty;
    public string RepoUrl { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? MainLanguage { get; set; }
    public string? ReadmeContent { get; set; }
    public string? AiSummary { get; set; }
    public string? TechStackJson { get; set; }
    public decimal? QualityScore { get; set; }
    public DateTimeOffset? LastSyncedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}
