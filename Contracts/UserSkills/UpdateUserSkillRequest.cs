namespace SWP_BE.Contracts.UserSkills;

public sealed record UpdateUserSkillRequest(
    string? Level,
    string? EvidenceUrl,
    string? EvidenceType);
