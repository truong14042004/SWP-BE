using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<NotificationListResponse>> GetMine(
        [FromQuery] bool? unreadOnly,
        [FromQuery] int? take,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var query = dbContext.Notifications
            .AsNoTracking()
            .Where(item => item.UserId == userId);

        if (unreadOnly == true)
        {
            query = query.Where(item => !item.IsRead);
        }

        var pageSize = Math.Clamp(take ?? 30, 1, 100);

        var items = await query
            .OrderByDescending(item => item.CreatedAt)
            .Take(pageSize)
            .Select(item => new NotificationItemResponse(
                item.Id,
                item.Type,
                item.Title,
                item.Message,
                item.LinkUrl,
                item.IsRead,
                item.CreatedAt,
                item.ReadAt))
            .ToListAsync(cancellationToken);

        var unreadCount = await dbContext.Notifications
            .AsNoTracking()
            .CountAsync(item => item.UserId == userId && !item.IsRead, cancellationToken);

        return Ok(new NotificationListResponse(items, unreadCount));
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var notification = await dbContext.Notifications
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);

        if (notification is null)
        {
            return NotFound();
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var now = DateTimeOffset.UtcNow;

        var unread = await dbContext.Notifications
            .Where(item => item.UserId == userId && !item.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in unread)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(value!);
    }
}

public sealed record NotificationItemResponse(
    Guid Id,
    string Type,
    string Title,
    string Message,
    string? LinkUrl,
    bool IsRead,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt);

public sealed record NotificationListResponse(
    IReadOnlyList<NotificationItemResponse> Items,
    int UnreadCount);
