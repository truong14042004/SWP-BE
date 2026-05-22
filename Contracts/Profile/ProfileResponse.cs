namespace SWP_BE.Contracts.Profile;

public sealed record ProfileResponse(
    Guid Id,
    Guid UserId,
    string? School,
    string? Major,
    int? Year,
    decimal? Gpa,
    Guid? TargetRoleId,
    string? TargetRoleName,
    string? GithubUsername,
    string? CareerGoal,
    int? PreferredLearningHoursPerWeek,
    string? CvUrl,
    string? CvName,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
