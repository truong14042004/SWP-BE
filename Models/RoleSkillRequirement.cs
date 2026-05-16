namespace SWP_BE.Models;

public sealed class RoleSkillRequirement
{
    public Guid Id { get; set; }
    public Guid CareerRoleId { get; set; }
    public Guid SkillId { get; set; }
    public string RequiredLevel { get; set; } = string.Empty;
    public int Priority { get; set; }
    public decimal Weight { get; set; } = 1m;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public CareerRole CareerRole { get; set; } = null!;
    public Skill Skill { get; set; } = null!;
}
