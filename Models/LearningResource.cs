namespace SWP_BE.Models;

public sealed class LearningResource
{
    public Guid Id { get; set; }
    public Guid? SkillId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? StorageObjectName { get; set; }
    public string? ContentType { get; set; }
    public long? FileSize { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public string? Difficulty { get; set; }
    public int? EstimatedHours { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Skill? Skill { get; set; }
}
