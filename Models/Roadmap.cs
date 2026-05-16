namespace SWP_BE.Models;

public sealed class Roadmap
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid CareerRoleId { get; set; }
    public Guid? SkillGapReportId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Draft";
    public decimal Progress { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public CareerRole CareerRole { get; set; } = null!;
    public SkillGapReport? SkillGapReport { get; set; }
}
