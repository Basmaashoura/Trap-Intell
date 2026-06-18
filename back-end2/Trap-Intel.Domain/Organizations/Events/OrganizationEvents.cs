using System;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Organizations.Events
{
    /// <summary>
    /// Domain event raised when an organization is created.
    /// </summary>
    public record OrganizationCreatedEvent(
        Guid OrganizationId,
        string Name,
        string Industry,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Domain event raised when an organization is activated.
    /// </summary>
    public record OrganizationActivatedEvent(
        Guid OrganizationId,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Domain event raised when an organization is suspended.
    /// </summary>
    public record OrganizationSuspendedEvent(
        Guid OrganizationId,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Domain event raised when an organization is deactivated.
    /// </summary>
    public record OrganizationDeactivatedEvent(
        Guid OrganizationId,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Domain event raised when an address is added to an organization.
    /// </summary>
    public record AddressAddedEvent(
        Guid OrganizationId,
        string Street,
        string City,
        string State,
        string PostalCode,
        string Country,
        string AddressType,
        DateTime OccurredOn) : IDomainEvent;

    /// <summary>
    /// Domain event raised when an address is removed from an organization.
    /// </summary>
    public record AddressRemovedEvent(
        Guid OrganizationId,
        string Street,
        string City,
        DateTime OccurredOn) : IDomainEvent;
}
