namespace SWP_BE.Services;

public interface IAuditLogService
{
    /// <summary>
    /// Ghi một bản ghi audit. Không ném lỗi ra ngoài để tránh làm hỏng
    /// luồng nghiệp vụ chính nếu việc ghi log thất bại.
    /// </summary>
    Task LogAsync(
        Guid actorUserId,
        string actorRole,
        string action,
        string entityType,
        Guid? entityId,
        Guid? targetUserId,
        string summary,
        object? metadata = null,
        CancellationToken cancellationToken = default);
}
