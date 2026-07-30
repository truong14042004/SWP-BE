using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Admin)]
[Route("api/admin/audit-logs")]
public sealed class AdminAuditLogsController(AppDbContext dbContext) : ControllerBase
{
    // GET /api/admin/audit-logs?action=&actorUserId=&targetUserId=&page=1&pageSize=50
    [HttpGet]
    [ProducesResponseType<AuditLogPageResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuditLogPageResponse>> GetAuditLogs(
        [FromQuery] string? action,
        [FromQuery] string? entityType,
        [FromQuery] Guid? actorUserId,
        [FromQuery] Guid? targetUserId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = dbContext.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(item => item.Action == action);
        }
        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(item => item.EntityType == entityType);
        }
        if (actorUserId.HasValue)
        {
            query = query.Where(item => item.ActorUserId == actorUserId.Value);
        }
        if (targetUserId.HasValue)
        {
            query = query.Where(item => item.TargetUserId == targetUserId.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(item => item.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new AuditLogResponse(
                item.Id,
                item.ActorUserId,
                item.ActorUser != null ? item.ActorUser.FullName : null,
                item.ActorRole,
                item.Action,
                item.EntityType,
                item.EntityId,
                item.TargetUserId,
                item.TargetUser != null ? item.TargetUser.FullName : null,
                item.Summary,
                item.MetadataJson,
                item.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(new AuditLogPageResponse(items, total, page, pageSize));
    }
}

public sealed record AuditLogResponse(
    Guid Id,
    Guid ActorUserId,
    string? ActorFullName,
    string ActorRole,
    string Action,
    string EntityType,
    Guid? EntityId,
    Guid? TargetUserId,
    string? TargetFullName,
    string Summary,
    string? MetadataJson,
    DateTimeOffset CreatedAt);

public sealed record AuditLogPageResponse(
    IReadOnlyList<AuditLogResponse> Items,
    int Total,
    int Page,
    int PageSize);
