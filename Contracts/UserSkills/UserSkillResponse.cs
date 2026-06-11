namespace SWP_BE.Contracts.UserSkills;

public sealed record UserSkillResponse(
    Guid Id,
    Guid SkillId,
    string SkillName,
    string SkillCategory,
    string Level,
    string? VerifiedLevel,
    string? EvidenceUrl,
    string? EvidenceType,
    string VerificationStatus,
    bool IsVerified,
    DateTimeOffset? VerifiedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
