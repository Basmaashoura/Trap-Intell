using Application.Abstractions.Notifications;
using SharedKernel;

namespace Application.Notifications.Common;

/// <summary>
/// Base class for domain event handlers that send notifications.
/// Provides a consistent pattern for building notification contexts from domain events.
/// </summary>
/// <typeparam name="TEvent">The domain event type.</typeparam>
public abstract class NotificationDomainEventHandler<TEvent> : IDomainEventHandler<TEvent>
    where TEvent : IDomainEvent
{
    protected readonly INotificationService NotificationService;

    protected NotificationDomainEventHandler(INotificationService notificationService)
    {
        NotificationService = notificationService;
    }

    /// <summary>
    /// The notification type code to use (e.g., "stress.high_detected").
    /// </summary>
    protected abstract string NotificationTypeCode { get; }

    /// <summary>
    /// Builds the notification context from the domain event.
    /// Override this to specify recipients, data, and other notification properties.
    /// </summary>
    /// <param name="domainEvent">The domain event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The notification context, or null to skip sending.</returns>
    protected abstract Task<NotificationContext?> BuildContextAsync(
        TEvent domainEvent,
        CancellationToken cancellationToken);

    public async Task Handle(TEvent domainEvent, CancellationToken cancellationToken)
    {
        NotificationContext? context = await BuildContextAsync(domainEvent, cancellationToken);

        if (context is null || !context.ShouldSend || context.RecipientUserIds.Count == 0)
        {
            return;
        }

        await NotificationService
            .Create(NotificationTypeCode)
            .FromContext(context)
            .SendAsync(cancellationToken);
    }
}
