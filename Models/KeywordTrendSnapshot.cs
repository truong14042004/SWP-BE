namespace SWP_BE.Models;

public sealed class KeywordTrendSnapshot
{
    public Guid Id { get; set; }
    public string Keyword { get; set; } = string.Empty;
    public Guid? SkillId { get; set; }
    public DateOnly SnapshotDate { get; set; }
    public int WindowDays { get; set; }
    public int JobCount { get; set; }
    public int TotalMentions { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Skill? Skill { get; set; }
}
