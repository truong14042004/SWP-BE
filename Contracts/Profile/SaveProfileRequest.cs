namespace SWP_BE.Contracts.Profile;

public sealed record SaveProfileRequest(
    string? School,
    string? Major,
    int? Year,
    decimal? Gpa,
    Guid? TargetRoleId,
    string? GithubUsername,
    string? CareerGoal,
    int? PreferredLearningHoursPerWeek);
