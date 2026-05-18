namespace SWP_BE.Models;

public sealed class RoadmapNodeResource
{
    public Guid Id { get; set; }
    public Guid RoadmapNodeId { get; set; }
    public Guid LearningResourceId { get; set; }
    public int OrderIndex { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public RoadmapNode RoadmapNode { get; set; } = null!;
    public LearningResource LearningResource { get; set; } = null!;
}
