using System;

namespace SWP_BE.Models;

public sealed class CounselorAssignment
{
    public Guid Id { get; set; }
    public Guid CounselorId { get; set; }
    public Guid StudentId { get; set; }
    public Guid AssignedByAdminId { get; set; }
    public string Status { get; set; } = "Active"; // Active, Inactive
    public string? Note { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User Counselor { get; set; } = null!;
    public User Student { get; set; } = null!;
    public User AssignedByAdmin { get; set; } = null!;
}
