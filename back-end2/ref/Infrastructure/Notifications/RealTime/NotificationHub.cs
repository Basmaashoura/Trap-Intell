using Application.Abstractions.Data;
using Application.Abstractions.Repositories;
using Domain.Notifications;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Notifications.RealTime;

/// <summary>
/// SignalR hub for real-time notifications.
/// </summary>
[Authorize]
public sealed class NotificationHub : Hub<INotificationHubClient>
{
    private readonly UserConnectionManager _connectionManager;
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(
        UserConnectionManager connectionManager,
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork,
        ILogger<NotificationHub> logger)
    {
        _connectionManager = connectionManager;
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        Guid? userId = Context.User?.GetUserId();

        if (userId.HasValue)
        {
            _connectionManager.AddConnection(userId.Value, Context.ConnectionId);

            // Add to user's personal group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId.Value}");

            // Add to role groups
            IEnumerable<string>? roles = Context.User?.GetRoles();
            if (roles is not null)
            {
                foreach (string role in roles)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"role-{role}");
                }
            }

            _logger.LogDebug(
                "User {UserId} connected to notification hub. ConnectionId: {ConnectionId}",
                userId.Value,
                Context.ConnectionId);

            // Send current unread count
            int unreadCount = await _notificationRepository.GetUnreadCountAsync(userId.Value);

            await Clients.Caller.UnreadCountUpdated(unreadCount);

            // Send connection established event
            await Clients.Caller.ConnectionEstablished(Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Guid? userId = Context.User?.GetUserId();

        _connectionManager.RemoveConnection(Context.ConnectionId);

        if (userId.HasValue)
        {
            _logger.LogDebug(
                "User {UserId} disconnected from notification hub. ConnectionId: {ConnectionId}",
                userId.Value,
                Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Marks a notification as read.
    /// </summary>
    public async Task MarkAsRead(Guid notificationId)
    {
        Guid? userId = Context.User?.GetUserId();

        if (!userId.HasValue)
        {
            return;
        }

        Notification? notification = await _notificationRepository.GetByIdAsync(
            notificationId,
            userId.Value);

        if (notification is not null)
        {
            notification.MarkAsRead();
            await _unitOfWork.SaveChangesAsync();

            // Notify all user's connections
            await Clients.Group($"user-{userId.Value}").NotificationRead(notificationId);

            // Update unread count
            int unreadCount = await _notificationRepository.GetUnreadCountAsync(userId.Value);
            await Clients.Group($"user-{userId.Value}").UnreadCountUpdated(unreadCount);
        }
    }

    /// <summary>
    /// Marks all notifications as read.
    /// </summary>
    public async Task MarkAllAsRead()
    {
        Guid? userId = Context.User?.GetUserId();

        if (!userId.HasValue)
        {
            return;
        }

        List<Notification> unreadNotifications = await _notificationRepository.GetUnreadAsync(userId.Value);

        foreach (Notification notification in unreadNotifications)
        {
            notification.MarkAsRead();
        }

        await _unitOfWork.SaveChangesAsync();

        // Notify all user's connections
        await Clients.Group($"user-{userId.Value}").AllNotificationsRead();
        await Clients.Group($"user-{userId.Value}").UnreadCountUpdated(0);
    }

    /// <summary>
    /// Dismisses a notification.
    /// </summary>
    public async Task Dismiss(Guid notificationId)
    {
        Guid? userId = Context.User?.GetUserId();

        if (!userId.HasValue)
        {
            return;
        }

        Notification? notification = await _notificationRepository.GetByIdAsync(
            notificationId,
            userId.Value);

        if (notification is not null)
        {
            bool wasUnread = !notification.IsRead;
            notification.Dismiss();
            await _unitOfWork.SaveChangesAsync();

            // Notify all user's connections
            await Clients.Group($"user-{userId.Value}").NotificationDismissed(notificationId);

            // Update unread count if was unread
            if (wasUnread)
            {
                int unreadCount = await _notificationRepository.GetUnreadCountAsync(userId.Value);
                await Clients.Group($"user-{userId.Value}").UnreadCountUpdated(unreadCount);
            }
        }
    }

    /// <summary>
    /// Marks a notification as unread.
    /// </summary>
    public async Task MarkAsUnread(Guid notificationId)
    {
        Guid? userId = Context.User?.GetUserId();

        if (!userId.HasValue)
        {
            return;
        }

        Notification? notification = await _notificationRepository.GetByIdAsync(
            notificationId,
            userId.Value);

        if (notification is not null && notification.IsRead)
        {
            notification.MarkAsUnread();
            await _unitOfWork.SaveChangesAsync();

            // Notify all user's connections
            await Clients.Group($"user-{userId.Value}").NotificationUnread(notificationId);

            // Update unread count
            int unreadCount = await _notificationRepository.GetUnreadCountAsync(userId.Value);
            await Clients.Group($"user-{userId.Value}").UnreadCountUpdated(unreadCount);
        }
    }

    /// <summary>
    /// Ping to keep connection alive.
    /// </summary>
    public async Task Ping()
    {
        await Clients.Caller.Pong(DateTime.UtcNow);
    }
}
