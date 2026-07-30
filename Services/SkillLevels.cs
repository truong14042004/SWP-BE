namespace SWP_BE.Services;

/// <summary>
/// Thang cấp độ dùng chung cho toàn hệ thống:
/// Beginner(1) &lt; Intermediate(2) &lt; Advanced(3) &lt; Expert(4).
/// Trước đây LevelRank/DifficultyRank/ParseLevel bị nhân bản ở 4 nơi với
/// luật khác nhau (thiếu "expert", thiếu nhãn tiếng Việt) — mọi nơi phải
/// dùng class này để hai luồng rule-based và AI chọn tài liệu giống nhau.
/// </summary>
public static class SkillLevels
{
    /// <summary>Rank cho tài nguyên không ghi độ khó / độ khó không nhận diện được.</summary>
    public const int UnknownDifficultyRank = 5;

    /// <summary>
    /// Quy đổi cấp độ kỹ năng (Level / VerifiedLevel / RequiredLevel) sang rank số.
    /// Trả 0 nếu không nhận diện được.
    /// </summary>
    public static int LevelRank(string? level) =>
        level?.Trim().ToLowerInvariant() switch
        {
            "expert" => 4,
            "advanced" => 3,
            // Dữ liệu cũ có thể còn Level = "Verified" (trạng thái bị trộn vào thang
            // cấp độ trước đây) — coi tương đương Advanced để không phá phép so sánh.
            "verified" => 3,
            "intermediate" => 2,
            "beginner" => 1,
            _ => 0
        };

    /// <summary>
    /// Quy đổi Difficulty của LearningResource sang rank số (nhận cả nhãn tiếng Việt).
    /// Trả <see cref="UnknownDifficultyRank"/> nếu không nhận diện được.
    /// </summary>
    public static int DifficultyRank(string? difficulty) =>
        difficulty?.Trim().ToLowerInvariant() switch
        {
            "beginner" or "basic" or "fundamental" or "fundamentals" or "cơ bản" => 1,
            "intermediate" or "trung cấp" => 2,
            "advanced" or "nâng cao" => 3,
            "expert" => 4,
            _ => UnknownDifficultyRank
        };

    /// <summary>
    /// Quy đổi Difficulty của tài nguyên sang cấp độ UserSkill chuẩn hoá.
    /// Expert là cấp độ hợp lệ — nếu ép xuống Advanced thì RoleSkillRequirement
    /// mức Expert không bao giờ được thoả mãn (rank 3 &lt; 4 vĩnh viễn).
    /// Trả null nếu không nhận diện được.
    /// </summary>
    public static string? DifficultyToLevel(string? difficulty) =>
        RankToDifficulty(DifficultyRank(difficulty));

    /// <summary>Quy đổi rank số ngược lại nhãn Difficulty chuẩn.</summary>
    public static string? RankToDifficulty(int rank) =>
        rank switch
        {
            1 => "Beginner",
            2 => "Intermediate",
            3 => "Advanced",
            4 => "Expert",
            _ => null
        };
}
