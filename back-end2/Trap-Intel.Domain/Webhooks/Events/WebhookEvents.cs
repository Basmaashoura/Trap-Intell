using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Webhooks.Enums;

namespace Trap_Intel.Domain.Webhooks.Events;

/// <summary>
/// Webhook created.
/// </summary>
public record WebhookCreatedEvent(
    Guid WebhookId,
    Guid OrganizationId,
    string Name,
    string Url,
    Guid CreatedByUserId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Webhook delivery succeeded.
/// </summary>
public record WebhookDeliverySucceededEvent(
    Guid WebhookId,
    Guid OrganizationId,
    WebhookEventType EventType,
    int ResponseStatusCode,
    TimeSpan Duration,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Webhook delivery failed.
/// </summary>
public record WebhookDeliveryFailedEvent(
    Guid WebhookId,
    Guid OrganizationId,
    WebhookEventType EventType,
    int? ResponseStatusCode,
    string ErrorMessage,
    int ConsecutiveFailures,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Webhook verified successfully.
/// </summary>
public record WebhookVerifiedEvent(
    Guid WebhookId,
    Guid OrganizationId,
    string Url,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Webhook verification failed.
/// </summary>
public record WebhookVerificationFailedEvent(
    Guid WebhookId,
    Guid OrganizationId,
    string Url,
    string Reason,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Webhook status changed.
/// </summary>
public record WebhookStatusChangedEvent(
    Guid WebhookId,
    Guid OrganizationId,
    WebhookStatus OldStatus,
    WebhookStatus NewStatus,
    string Reason,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Webhook auto-disabled due to consecutive failures.
/// </summary>
public record WebhookAutoDisabledEvent(
    Guid WebhookId,
    Guid OrganizationId,
    int ConsecutiveFailures,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Webhook deleted.
/// </summary>
public record WebhookDeletedEvent(
    Guid WebhookId,
    Guid OrganizationId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Webhook URL updated.
/// </summary>
public record WebhookUrlUpdatedEvent(
    Guid WebhookId,
    Guid OrganizationId,
    string OldUrl,
    string NewUrl,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Webhook secret rotated.
/// </summary>
public record WebhookSecretRotatedEvent(
    Guid WebhookId,
    Guid OrganizationId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Webhook subscribed events updated.
/// </summary>
public record WebhookEventsUpdatedEvent(
    Guid WebhookId,
    Guid OrganizationId,
    List<WebhookEventType> SubscribedEvents,
    DateTime OccurredOn) : IDomainEvent;
