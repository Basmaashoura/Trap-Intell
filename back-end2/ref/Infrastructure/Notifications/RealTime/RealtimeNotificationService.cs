using Application.Abstractions.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Notifications.RealTime;

/// <summary>
/// Service for sending real-time notifications via SignalR.
/// </summary>
internal sealed class RealtimeNotificationService : IRealtimeNotificationService
{
    private readonly IHubContext<NotificationHub, INotificationHubClient> _hubContext;
    private readonly UserConnectionManager _connectionManager;

    public RealtimeNotificationService(
        IHubContext<NotificationHub, INotificationHubClient> hubContext,
        UserConnectionManager connectionManager)
    {
        _hubContext = hubContext;
        _connectionManager = connectionManager;
    }

    public async Task SendToUserAsync(
        Guid userId,
        RealtimeNotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> connections = _connectionManager.GetConnections(userId);

        if (connections.Count > 0)
        {
            await _hubContext.Clients
                .Clients(connections)
                .ReceiveNotification(message);
        }
    }

    public async Task SendToUsersAsync(
        IEnumerable<Guid> userIds,
        RealtimeNotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> connections = _connectionManager.GetConnections(userIds);

        if (connections.Count > 0)
        {
            await _hubContext.Clients
                .Clients(connections)
                .ReceiveNotification(message);
        }
    }

    public async Task SendToRoleAsync(
        string roleName,
        RealtimeNotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        // Send to role group
        await _hubContext.Clients
            .Group($"role-{roleName}")
            .ReceiveNotification(message);
    }

    public async Task SendToAllAsync(
        RealtimeNotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.All.ReceiveNotification(message);
    }

    public async Task NotifyReadAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> connections = _connectionManager.GetConnections(userId);

        if (connections.Count > 0)
        {
            await _hubContext.Clients
                .Clients(connections)
                .NotificationRead(notificationId);
        }
    }

    public async Task NotifyAllReadAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> connections = _connectionManager.GetConnections(userId);

        if (connections.Count > 0)
        {
            await _hubContext.Clients
                .Clients(connections)
                .AllNotificationsRead();
        }
    }

    public async Task NotifyUnreadCountAsync(
        Guid userId,
        int count,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> connections = _connectionManager.GetConnections(userId);

        if (connections.Count > 0)
        {
            await _hubContext.Clients
                .Clients(connections)
                .UnreadCountUpdated(count);
        }
    }
}
