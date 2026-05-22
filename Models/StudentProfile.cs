namespace SWP_BE.Models;

public sealed class StudentProfile
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
    public string? CvUrl { get; set; }
    public string? CvName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public CareerRole? TargetRole { get; set; }
}
