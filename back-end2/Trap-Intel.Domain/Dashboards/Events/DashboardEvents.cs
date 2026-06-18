using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Dashboards.Enums;

namespace Trap_Intel.Domain.Dashboards.Events;

/// <summary>
/// Dashboard created.
/// </summary>
public record DashboardCreatedEvent(
    Guid DashboardId,
    Guid UserId,
    Guid OrganizationId,
    string Name,
    DashboardType Type,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Dashboard widget added.
/// </summary>
public record DashboardWidgetAddedEvent(
    Guid DashboardId,
    Guid UserId,
    Guid WidgetId,
    WidgetType WidgetType,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Dashboard widget removed.
/// </summary>
public record DashboardWidgetRemovedEvent(
    Guid DashboardId,
    Guid UserId,
    Guid WidgetId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Dashboard set as default.
/// </summary>
public record DashboardSetAsDefaultEvent(
    Guid DashboardId,
    Guid UserId,
    Guid OrganizationId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Dashboard shared with another user.
/// </summary>
public record DashboardSharedEvent(
    Guid DashboardId,
    Guid OwnerUserId,
    Guid SharedWithUserId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Dashboard unshared from a user.
/// </summary>
public record DashboardUnsharedEvent(
    Guid DashboardId,
    Guid OwnerUserId,
    Guid UnsharedFromUserId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Dashboard duplicated.
/// </summary>
public record DashboardDuplicatedEvent(
    Guid NewDashboardId,
    Guid SourceDashboardId,
    Guid TargetUserId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Dashboard deleted.
/// </summary>
public record DashboardDeletedEvent(
    Guid DashboardId,
    Guid UserId,
    DateTime OccurredOn) : IDomainEvent;
