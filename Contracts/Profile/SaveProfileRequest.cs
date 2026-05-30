using Microsoft.AspNetCore.Http;

namespace SWP_BE.Contracts.Profile;

public sealed class SaveProfileRequest
{
    public string? School { get; set; }

    public string? Major { get; set; }

    public int? Year { get; set; }

    public decimal? Gpa { get; set; }

    public Guid? TargetRoleId { get; set; }

    public string? GithubUsername { get; set; }

    public string? CareerGoal { get; set; }

    public int? PreferredLearningHoursPerWeek { get; set; }

    public IFormFile? CvFile { get; set; }
}
