namespace SWP_BE.Models;

/// <summary>
/// Bản ghi nhật ký audit cho các hành động duyệt/xác thực quan trọng
/// (duyệt kỹ năng, duyệt khung lộ trình, duyệt tiến độ node...).
/// </summary>
public sealed class AuditLog
{
    public Guid Id { get; set; }

    /// <summary>Người thực hiện hành động (counselor/mentor/admin).</summary>
    public Guid ActorUserId { get; set; }

    /// <summary>Vai trò của người thực hiện tại thời điểm hành động.</summary>
    public string ActorRole { get; set; } = string.Empty;

    /// <summary>Mã hành động, ví dụ: SkillVerified, RoadmapApprovalRejected.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Loại thực thể bị tác động, ví dụ: UserSkill, RoadmapApprovalRequest.</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Id thực thể bị tác động.</summary>
    public Guid? EntityId { get; set; }

    /// <summary>Sinh viên bị ảnh hưởng (nếu có).</summary>
    public Guid? TargetUserId { get; set; }

    /// <summary>Mô tả ngắn gọn, dễ đọc.</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>Chi tiết bổ sung dưới dạng JSON (tùy chọn).</summary>
    public string? MetadataJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public User? ActorUser { get; set; }
    public User? TargetUser { get; set; }
}
