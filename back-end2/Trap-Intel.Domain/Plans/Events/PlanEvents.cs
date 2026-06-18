using System;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Plans
{
    /// <summary>
    /// Domain events for Plan aggregate.
    /// </summary>

    public record PlanCreatedEvent(
        Guid PlanId,
        string Name,
        PlanType Type,
        DateTime OccurredOn) : IDomainEvent;

    public record PlanActivatedEvent(
        Guid PlanId,
        DateTime OccurredOn) : IDomainEvent;

    public record PlanDeactivatedEvent(
        Guid PlanId,
        DateTime OccurredOn) : IDomainEvent;

    public record PlanPricingAddedEvent(
        Guid PlanId,
        BillingCycle Cycle,
        decimal Price,
        DateTime OccurredOn) : IDomainEvent;

    public record AIFeaturesEnabledEvent(
        Guid PlanId,
        DateTime OccurredOn) : IDomainEvent;

    public record ThreatIntelligenceEnabledEvent(
        Guid PlanId,
        DateTime OccurredOn) : IDomainEvent;

    public record PlanQuotaDefinitionUpdatedEvent(
        Guid PlanId,
        int MaxHoneypots,
        decimal MaxStorageGb,
        int MaxUsers,
        DateTime OccurredOn) : IDomainEvent;

    public record PlanFeaturesUpdatedEvent(
        Guid PlanId,
        int TotalFeatures,
        int EnabledFeatures,
        DateTime OccurredOn) : IDomainEvent;
}
