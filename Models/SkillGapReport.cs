namespace SWP_BE.Models;

public sealed class SkillGapReport
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid CareerRoleId { get; set; }
    public decimal MatchScore { get; set; }
    public string? Summary { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public CareerRole CareerRole { get; set; } = null!;
}
