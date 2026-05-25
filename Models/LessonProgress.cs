namespace SWP_BE.Models;

/// <summary>
/// Ghi nhan mot sinh vien da hoan thanh mot lesson (LearningResource) thuoc mot RoadmapNode.
/// Unique tren (UserId, RoadmapNodeId, LearningResourceId). Xoa row = unmark.
/// </summary>
public sealed class LessonProgress
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid RoadmapNodeId { get; set; }
    public Guid LearningResourceId { get; set; }
    public DateTimeOffset CompletedAt { get; set; }

    public User User { get; set; } = null!;
    public RoadmapNode RoadmapNode { get; set; } = null!;
    public LearningResource LearningResource { get; set; } = null!;
}
