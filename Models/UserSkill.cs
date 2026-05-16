using System;
using System.Collections.Generic;

namespace SWP_BE.Models;

public partial class UserSkill
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid SkillId { get; set; }

    public string Level { get; set; } = null!;

    public string? EvidenceUrl { get; set; }

    public string? EvidenceType { get; set; }

    public bool IsVerified { get; set; }

    public Guid? VerifiedByUserId { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Skill Skill { get; set; } = null!;

    public virtual User User { get; set; } = null!;

    public virtual User? VerifiedByUser { get; set; }
}
