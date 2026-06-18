using System;
using System.Collections.Generic;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Roles;

/// <summary>
/// Domain events for the Role aggregate.
/// </summary>

/// <summary>
/// Raised when a new role is created.
/// </summary>
public record RoleCreatedEvent(
    Guid RoleId,
    Guid? OrganizationId,
    string Name,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Raised when permissions for a role are updated.
/// </summary>
public record RolePermissionsUpdatedEvent(
    Guid RoleId,
    Guid? OrganizationId,
    IReadOnlyList<string> AddedPermissions,
    IReadOnlyList<string> RemovedPermissions,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Raised when a role is deactivated.
/// </summary>
public record RoleDeactivatedEvent(
    Guid RoleId,
    Guid? OrganizationId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Raised when a role is activated.
/// </summary>
public record RoleActivatedEvent(
    Guid RoleId,
    Guid? OrganizationId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Raised when a role is soft-deleted.
/// </summary>
public record RoleDeletedEvent(
    Guid RoleId,
    Guid? OrganizationId,
    DateTime OccurredOn) : IDomainEvent;
