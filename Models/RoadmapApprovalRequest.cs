using System;

namespace SWP_BE.Models;

public sealed class RoadmapApprovalRequest
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid? CounselorId { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    public string PayloadJson { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public Guid? MaterializedRoadmapId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User Student { get; set; } = null!;
    public User? Counselor { get; set; }
    public Roadmap? MaterializedRoadmap { get; set; }
}
