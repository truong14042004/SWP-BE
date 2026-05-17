namespace SWP_BE.Contracts.UserSkills;

public sealed record UserSkillResponse(
    Guid Id,
    Guid SkillId,
    string SkillName,
    string SkillCategory,
    string Level,
    string? EvidenceUrl,
    string? EvidenceType,
    bool IsVerified,
    DateTimeOffset? VerifiedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
