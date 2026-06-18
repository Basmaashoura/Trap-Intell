using Application.Abstractions.Data;
using Application.Abstractions.Notifications;
using Domain.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Notifications;

/// <summary>
/// Dispatches notifications to multiple channels.
/// </summary>
internal sealed class NotificationDispatcher : INotificationDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(
        IServiceProvider serviceProvider,
        IUnitOfWork unitOfWork,
        ILogger<NotificationDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Dictionary<NotificationChannel, bool>> DispatchAsync(
        Notification notification,
        NotificationChannel channels,
        CancellationToken cancellationToken = default)
    {
        // Get all registered channels
        IEnumerable<INotificationChannel> channelHandlers = _serviceProvider.GetServices<INotificationChannel>();

        var dispatchTasks = new List<Task<(NotificationChannel Channel, bool Success)>>();

        foreach (INotificationChannel handler in channelHandlers)
        {
            // Skip if channel not requested
            if (!channels.HasFlag(handler.Channel))
            {
                continue;
            }

            // Skip if channel not available
            if (!handler.IsAvailable)
            {
                _logger.LogDebug(
                    "Skipping channel {Channel} for notification {NotificationId} - not available",
                    handler.Channel,
                    notification.Id);
                continue;
            }

            // Create delivery record
            var delivery = NotificationDelivery.Create(
                notification.Id,
                handler.Channel);

            notification.AddDelivery(delivery);

            dispatchTasks.Add(DispatchToChannelAsync(handler, notification, delivery, cancellationToken));
        }

        (NotificationChannel Channel, bool Success)[] results = await Task.WhenAll(dispatchTasks);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var resultDictionary = new Dictionary<NotificationChannel, bool>();
        foreach ((NotificationChannel Channel, bool Success) in results)
        {
            resultDictionary[Channel] = Success;
        }

        return resultDictionary;
    }

    private async Task<(NotificationChannel Channel, bool Success)> DispatchToChannelAsync(
        INotificationChannel handler,
        Notification notification,
        NotificationDelivery delivery,
        CancellationToken cancellationToken)
    {
        try
        {
            bool success = await handler.DeliverAsync(notification, cancellationToken);

            if (success)
            {
                delivery.MarkAsSent();
                _logger.LogDebug(
                    "Successfully delivered notification {NotificationId} via {Channel}",
                    notification.Id,
                    handler.Channel);
                return (handler.Channel, true);
            }
            else
            {
                delivery.MarkAsFailed("Delivery returned false");
                _logger.LogWarning(
                    "Failed to deliver notification {NotificationId} via {Channel}",
                    notification.Id,
                    handler.Channel);
                return (handler.Channel, false);
            }
        }
        catch (Exception ex)
        {
            delivery.MarkAsFailed(ex.Message, DateTime.UtcNow.AddMinutes(5));
            _logger.LogError(ex,
                "Error delivering notification {NotificationId} via {Channel}",
                notification.Id,
                handler.Channel);
            return (handler.Channel, false);
        }
    }
}
