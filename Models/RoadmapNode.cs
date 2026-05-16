namespace SWP_BE.Models;

public sealed class RoadmapNode
{
    public Guid Id { get; set; }
    public Guid RoadmapId { get; set; }
    public Guid? SkillId { get; set; }
    public Guid? LearningResourceId { get; set; }
    public Guid? PrerequisiteNodeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string NodeType { get; set; } = string.Empty;
    public string Status { get; set; } = "NotStarted";
    public int OrderIndex { get; set; }
    public int? EstimatedHours { get; set; }
    public int Priority { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Roadmap Roadmap { get; set; } = null!;
    public Skill? Skill { get; set; }
    public LearningResource? LearningResource { get; set; }
    public RoadmapNode? PrerequisiteNode { get; set; }
}
