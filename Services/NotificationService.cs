using Microsoft.AspNetCore.SignalR;
using SWP_BE.Data;
using SWP_BE.Hubs;
using SWP_BE.Models;

namespace SWP_BE.Services;

public interface INotificationService
{
    Task SendNotificationAsync(Guid userId, string type, string title, string message, string? linkUrl = null, string? payloadJson = null, CancellationToken cancellationToken = default);
}

public sealed class NotificationService : INotificationService
{
    private readonly AppDbContext _dbContext;
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationService(AppDbContext dbContext, IHubContext<NotificationHub> hubContext)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
    }

    public async Task SendNotificationAsync(Guid userId, string type, string title, string message, string? linkUrl = null, string? payloadJson = null, CancellationToken cancellationToken = default)
    {
        // 1. Create notification model
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            LinkUrl = linkUrl,
            PayloadJson = payloadJson,
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // 2. Save to database
        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 3. Push to SignalR clients (connected users matching the userId)
        var clientNotification = new
        {
            id = notification.Id,
            type = notification.Type,
            title = notification.Title,
            message = notification.Message,
            linkUrl = notification.LinkUrl,
            payloadJson = notification.PayloadJson,
            isRead = notification.IsRead,
            createdAt = notification.CreatedAt
        };

        await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", clientNotification, cancellationToken);
    }
}
