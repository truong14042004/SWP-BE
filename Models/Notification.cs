namespace SWP_BE.Models;

public sealed class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Type { get; set; } = string.Empty; // RoadmapReviewApproved | RoadmapReviewRejected | RoadmapReviewRequested | etc
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }       // FE route hash, e.g. "#roadmap"
    public string? PayloadJson { get; set; }   // optional structured data
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }

    public User User { get; set; } = null!;
}
