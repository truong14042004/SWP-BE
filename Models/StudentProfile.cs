using System;
using System.Collections.Generic;

namespace SWP_BE.Models;

public partial class StudentProfile
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string? School { get; set; }

    public string? Major { get; set; }

    public int? Year { get; set; }

    public decimal? Gpa { get; set; }

    public Guid? TargetRoleId { get; set; }

    public string? GithubUsername { get; set; }

    public string? CareerGoal { get; set; }

    public int? PreferredLearningHoursPerWeek { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual CareerRole? TargetRole { get; set; }

    public virtual User User { get; set; } = null!;
}
