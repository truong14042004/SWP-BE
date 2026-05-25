namespace SWP_BE.Models;

/// <summary>
/// Ket qua AI quet evidence (vi du GitHub repo) cho mot RoadmapNodeReviewRequest.
/// Mentor bam nut "Review bang AI" -> service goi Gemini -> luu summary o day.
/// 1-1 voi RoadmapNodeReviewRequest.
/// </summary>
public sealed class AiReviewSummary
{
    public Guid Id { get; set; }

    public Guid ReviewRequestId { get; set; }

    /// <summary>
    /// Mentor da trigger AI review (nullable phong khi Admin chay).
    /// </summary>
    public Guid? GeneratedByUserId { get; set; }

    /// <summary>
    /// Loai evidence ma AI da quet (sao chep tu request tai thoi diem quet).
    /// Hien tai chi ho tro "GitRepository".
    /// </summary>
    public string EvidenceType { get; set; } = string.Empty;

    /// <summary>
    /// URL goc cua evidence luc quet (de doi chieu neu sau nay student doi).
    /// </summary>
    public string EvidenceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Mo hinh Gemini da dung (vd "gemini-3.1-flash-lite").
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Tong so token tieu thu (neu Gemini tra ve).
    /// </summary>
    public int? TokensUsed { get; set; }

    /// <summary>
    /// Mang JSON cac tech / framework / lib AI phat hien.
    /// </summary>
    public string TechStackJson { get; set; } = "[]";

    /// <summary>
    /// Mang JSON cac diem manh.
    /// </summary>
    public string StrengthsJson { get; set; } = "[]";

    /// <summary>
    /// Mang JSON cac diem yeu / anti-pattern / rui ro.
    /// </summary>
    public string WeaknessesJson { get; set; } = "[]";

    /// <summary>
    /// Mang JSON cac cau hoi mentor co the hoi student.
    /// </summary>
    public string SuggestedQuestionsJson { get; set; } = "[]";

    /// <summary>
    /// JSON danh gia evidence co thuc su the hien duoc skill cua node hay khong.
    /// Vi du: { "matchesNode": true, "reason": "...", "missingAspects": [...] }
    /// </summary>
    public string SkillMappingJson { get; set; } = "{}";

    /// <summary>
    /// Tom tat ngan goi (1-2 cau) AI dua ra.
    /// </summary>
    public string? OverallSummary { get; set; }

    public DateTimeOffset GeneratedAt { get; set; }

    public RoadmapNodeReviewRequest ReviewRequest { get; set; } = null!;
    public User? GeneratedByUser { get; set; }
}
