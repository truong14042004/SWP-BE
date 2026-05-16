using System;
using System.Collections.Generic;

namespace SWP_BE.Models;

public partial class RoadmapNode
{
    public Guid Id { get; set; }

    public Guid RoadmapId { get; set; }

    public Guid? SkillId { get; set; }

    public Guid? LearningResourceId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string NodeType { get; set; } = null!;

    public string Status { get; set; } = null!;

    public int OrderIndex { get; set; }

    public int? EstimatedHours { get; set; }

    public int Priority { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Guid? PrerequisiteNodeId { get; set; }

    public virtual ICollection<RoadmapNode> InversePrerequisiteNode { get; set; } = new List<RoadmapNode>();

    public virtual LearningResource? LearningResource { get; set; }

    public virtual RoadmapNode? PrerequisiteNode { get; set; }

    public virtual Roadmap Roadmap { get; set; } = null!;

    public virtual Skill? Skill { get; set; }
}
