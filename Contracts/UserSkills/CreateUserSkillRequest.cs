namespace SWP_BE.Contracts.UserSkills;

public sealed record CreateUserSkillRequest(
    Guid SkillId,
    string Level,
    string? EvidenceUrl,
    string? EvidenceType);
