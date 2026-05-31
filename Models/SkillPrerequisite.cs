namespace SWP_BE.Models;

public sealed class SkillPrerequisite
{
    public Guid Id { get; set; }
    public Guid SkillId { get; set; }
    public Guid PrerequisiteSkillId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Skill Skill { get; set; } = null!;
    public Skill PrerequisiteSkill { get; set; } = null!;
}
