using SWP_BE.Models;

namespace SWP_BE.Services;

/// <summary>
/// Ngữ cảnh của một node cần đảm bảo tối thiểu số tài nguyên học tập.
/// </summary>
/// <param name="NodeId">Id của RoadmapNode.</param>
/// <param name="SkillId">Skill gắn với node (nếu có) để tái sử dụng tài nguyên theo kỹ năng.</param>
/// <param name="Topic">Từ khóa chủ đề dùng để sinh link tài nguyên (thường là tên kỹ năng).</param>
/// <param name="ExistingCount">Số tài nguyên hiện có của node.</param>
public sealed record NodeResourceContext(Guid NodeId, Guid? SkillId, string Topic, int ExistingCount);

/// <summary>
/// Đảm bảo mỗi technical node có tối thiểu N tài nguyên học tập (FR2.3 — ít nhất 2 link).
/// Khi node thiếu, service tự sinh link curated (Video/YouTube + Documentation) theo chủ đề
/// và get-or-create idempotent vào bảng LearningResources.
/// </summary>
public interface IRoadmapResourceProvisioner
{
    /// <summary>
    /// Với mỗi node thiếu tài nguyên, tạo/lấy lại các LearningResource bổ sung (chưa SaveChanges —
    /// caller chịu trách nhiệm lưu cùng các thay đổi khác) và trả về danh sách resource id cần gắn
    /// thêm cho từng node, theo thứ tự ưu tiên.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>> EnsureMinimumResourcesAsync(
        IReadOnlyList<NodeResourceContext> nodes,
        int minResources,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}
