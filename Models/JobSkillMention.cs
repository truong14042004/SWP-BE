namespace SWP_BE.Models;

public sealed class JobSkillMention
{
    public Guid Id { get; set; }
    public Guid JobPostId { get; set; }
    public string Keyword { get; set; } = string.Empty;
    public Guid? SkillId { get; set; }
    public int MentionCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public JobPost JobPost { get; set; } = null!;
    public Skill? Skill { get; set; }
}
