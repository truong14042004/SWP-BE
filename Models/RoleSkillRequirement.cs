using System;
using System.Collections.Generic;

namespace SWP_BE.Models;

public partial class RoleSkillRequirement
{
    public Guid Id { get; set; }

    public Guid CareerRoleId { get; set; }

    public Guid SkillId { get; set; }

    public string RequiredLevel { get; set; } = null!;

    public int Priority { get; set; }

    public decimal Weight { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual CareerRole CareerRole { get; set; } = null!;

    public virtual Skill Skill { get; set; } = null!;
}
