using MediatR;
using Microsoft.Extensions.Logging;
using Trap_Intel.Domain.Notifications.Events;

namespace Trap_Intel.Application.Notifications.Events;

internal sealed class NotificationCreatedDomainEventHandler : INotificationHandler<NotificationCreatedDomainEvent>
{
    private readonly ILogger<NotificationCreatedDomainEventHandler> _logger;

    public NotificationCreatedDomainEventHandler(ILogger<NotificationCreatedDomainEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(NotificationCreatedDomainEvent notificationEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Domain event handled: Notification {NotificationId} created for User {UserId}", 
            notificationEvent.NotificationId, notificationEvent.UserId);

        // Channels are dispatched by NotificationDispatcher directly.
        // This handler remains for observability only to avoid recursive redispatch.
        return Task.CompletedTask;
    }
}
