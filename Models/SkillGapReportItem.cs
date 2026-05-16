namespace SWP_BE.Models;

public sealed class SkillGapReportItem
{
    public Guid Id { get; set; }
    public Guid SkillGapReportId { get; set; }
    public Guid SkillId { get; set; }
    public string? CurrentLevel { get; set; }
    public string RequiredLevel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string? Recommendation { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public SkillGapReport SkillGapReport { get; set; } = null!;
    public Skill Skill { get; set; } = null!;
}
