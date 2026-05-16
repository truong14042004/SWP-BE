namespace SWP_BE.Models;

public sealed class UserSkill
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SkillId { get; set; }
    public string Level { get; set; } = string.Empty;
    public string? EvidenceUrl { get; set; }
    public string? EvidenceType { get; set; }
    public bool IsVerified { get; set; }
    public Guid? VerifiedByUserId { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public Skill Skill { get; set; } = null!;
    public User? VerifiedByUser { get; set; }
}
