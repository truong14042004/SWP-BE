namespace SWP_BE.Models;

public sealed class GithubRepositorySkill
{
    public Guid Id { get; set; }
    public Guid GithubRepositoryId { get; set; }
    public Guid SkillId { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public string? EvidenceText { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public GithubRepository GithubRepository { get; set; } = null!;
    public Skill Skill { get; set; } = null!;
}
