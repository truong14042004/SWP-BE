namespace SWP_BE.Contracts.UserSkills;

public sealed record SubmitUserSkillEvidenceRequest(
    string EvidenceUrl,
    string EvidenceType);
