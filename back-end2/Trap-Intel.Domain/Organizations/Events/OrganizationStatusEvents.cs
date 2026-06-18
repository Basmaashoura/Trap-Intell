using System;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Organizations.Events
{
    /// <summary>
    /// Domain event raised when organization is approved.
    /// </summary>
    public record OrganizationApprovedEvent(
        Guid OrganizationId,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Domain event raised when organization is rejected.
    /// </summary>
    public record OrganizationRejectedEvent(
        Guid OrganizationId,
        string Reason,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Domain event raised when organization info is updated.
    /// </summary>
    public record OrganizationInfoUpdatedEvent(
        Guid OrganizationId,
        string ChangedFields,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Domain event raised when organization is deleted.
    /// </summary>
    public record OrganizationDeletedEvent(
        Guid OrganizationId,
        string Reason,
        DateTime OccurredOn) : IDomainEvent;
}
