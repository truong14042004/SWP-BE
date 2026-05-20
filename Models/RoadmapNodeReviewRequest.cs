namespace SWP_BE.Models;

public sealed class RoadmapNodeReviewRequest
{
    public Guid Id { get; set; }
    public Guid RoadmapNodeId { get; set; }
    public Guid StudentId { get; set; }
    public Guid ReviewerId { get; set; }
    public string ReviewerRole { get; set; } = string.Empty; // AcademicCounselor | IndustryMentor
    public string Status { get; set; } = "Pending"; // Pending | Approved | Rejected | Cancelled
    public string? StudentNote { get; set; }
    public string? ReviewerNote { get; set; }
    public string? EvidenceUrl { get; set; }
    public string? EvidenceType { get; set; } // GitRepository | ZipArchive | Document | Image
    public string? EvidenceFileName { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? RespondedAt { get; set; }

    public RoadmapNode RoadmapNode { get; set; } = null!;
    public User Student { get; set; } = null!;
    public User Reviewer { get; set; } = null!;
}
