using System;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Subscriptions
{
    /// <summary>
    /// Domain events for Subscription aggregate.
    /// </summary>

    public record SubscriptionCreatedEvent(
        Guid SubscriptionId,
        Guid OrganizationId,
        Guid PlanId,
        DateTime OccurredOn) : IDomainEvent;

    public record SubscriptionActivatedEvent(
        Guid SubscriptionId,
        DateTime OccurredOn) : IDomainEvent;

    public record SubscriptionSuspendedEvent(
        Guid SubscriptionId,
        DateTime OccurredOn) : IDomainEvent;

    public record SubscriptionCancelledEvent(
        Guid SubscriptionId,
        string Reason,
        DateTime OccurredOn) : IDomainEvent;

    public record SubscriptionUsageUpdatedEvent(
        Guid SubscriptionId,
        int HoneypotsUsed,
        decimal StorageUsedGb,
        DateTime OccurredOn) : IDomainEvent;

    public record SubscriptionRenewedEvent(
        Guid SubscriptionId,
        DateTime NewStartDate,
        DateTime NewEndDate,
        DateTime OccurredOn) : IDomainEvent;

    public record SubscriptionPlanChangedEvent(
        Guid SubscriptionId,
        Guid OldPlanId,
        Guid NewPlanId,
        decimal NewPrice,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Event raised when a subscription expires.
    /// </summary>
    public record SubscriptionExpiredEvent(
        Guid SubscriptionId,
        DateTime ExpirationDate,
        DateTime OccurredOn) : IDomainEvent;
}
