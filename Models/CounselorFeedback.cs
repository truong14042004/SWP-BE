namespace SWP_BE.Models;

public sealed class CounselorFeedback
{
    public Guid Id { get; set; }
    public Guid CounselorId { get; set; }
    public Guid StudentId { get; set; }
    public Guid? RoadmapId { get; set; }
    public Guid? SkillGapReportId { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User Counselor { get; set; } = null!;
    public User Student { get; set; } = null!;
    public Roadmap? Roadmap { get; set; }
    public SkillGapReport? SkillGapReport { get; set; }
}
