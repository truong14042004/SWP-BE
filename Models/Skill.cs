using System;
using System.Collections.Generic;

namespace SWP_BE.Models;

public partial class Skill
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Category { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<GithubRepositorySkill> GithubRepositorySkills { get; set; } = new List<GithubRepositorySkill>();

    public virtual ICollection<LearningResource> LearningResources { get; set; } = new List<LearningResource>();

    public virtual ICollection<RoadmapNode> RoadmapNodes { get; set; } = new List<RoadmapNode>();

    public virtual ICollection<RoleSkillRequirement> RoleSkillRequirements { get; set; } = new List<RoleSkillRequirement>();

    public virtual ICollection<SkillGapReportItem> SkillGapReportItems { get; set; } = new List<SkillGapReportItem>();

    public virtual ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();
}
