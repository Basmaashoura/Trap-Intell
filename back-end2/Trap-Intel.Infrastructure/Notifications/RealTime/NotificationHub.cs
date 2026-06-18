using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Trap_Intel.Infrastructure.Notifications.RealTime;

[Authorize]
public class NotificationHub : Hub<INotificationHubClient>
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrEmpty(userId))
        {
            _logger.LogInformation("SignalR connection established for user {UserId}", userId);

            // Add user to their specific group for targeted messages
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");

            // If we have an organization ID in the claims
            var orgId = Context.User?.FindFirstValue("organizationId");
            if (!string.IsNullOrEmpty(orgId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Org_{orgId}");
            }
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrEmpty(userId))
        {
            _logger.LogInformation("SignalR connection closed for user {UserId}", userId);

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");

            var orgId = Context.User?.FindFirstValue("organizationId");
            if (!string.IsNullOrEmpty(orgId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Org_{orgId}");
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}
