using System;
using System.Collections.Generic;

namespace SWP_BE.Models;

public partial class LearningResource
{
    public Guid Id { get; set; }

    public Guid? SkillId { get; set; }

    public string Title { get; set; } = null!;

    public string Url { get; set; } = null!;

    public string ResourceType { get; set; } = null!;

    public string? Difficulty { get; set; }

    public int? EstimatedHours { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<RoadmapNode> RoadmapNodes { get; set; } = new List<RoadmapNode>();

    public virtual Skill? Skill { get; set; }
}
