using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SWP_BE.Hubs;

[Authorize]
public sealed class NotificationHub : Hub
{
}
