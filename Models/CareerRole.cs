using System;
using System.Collections.Generic;

namespace SWP_BE.Models;

public partial class CareerRole
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? Level { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Roadmap> Roadmaps { get; set; } = new List<Roadmap>();

    public virtual ICollection<RoleSkillRequirement> RoleSkillRequirements { get; set; } = new List<RoleSkillRequirement>();

    public virtual ICollection<SkillGapReport> SkillGapReports { get; set; } = new List<SkillGapReport>();

    public virtual ICollection<StudentProfile> StudentProfiles { get; set; } = new List<StudentProfile>();
}
