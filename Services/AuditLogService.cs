using System.Text.Json;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Services;

public sealed class AuditLogService(
    AppDbContext dbContext,
    ILogger<AuditLogService> logger) : IAuditLogService
{
    public async Task LogAsync(
        Guid actorUserId,
        string actorRole,
        string action,
        string entityType,
        Guid? entityId,
        Guid? targetUserId,
        string summary,
        object? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entry = new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorUserId,
                ActorRole = actorRole,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                TargetUserId = targetUserId,
                Summary = summary,
                MetadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata),
                CreatedAt = DateTimeOffset.UtcNow
            };

            dbContext.AuditLogs.Add(entry);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Audit logging must never break the main business flow.
            logger.LogError(ex, "Không thể ghi audit log: {Action} {EntityType} {EntityId}", action, entityType, entityId);
        }
    }
}
