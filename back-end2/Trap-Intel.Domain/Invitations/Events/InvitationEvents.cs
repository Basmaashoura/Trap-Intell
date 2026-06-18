using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Identity;
using System;

namespace Trap_Intel.Domain.Invitations.Events;

/// <summary>
/// Invitation created and sent.
/// </summary>
public record InvitationCreatedEvent(
    Guid InvitationId,
    Guid OrganizationId,
    string Email,
    Guid RoleId,
    Guid InvitedByUserId,
    DateTime ExpiresAt,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Invitation accepted by invitee.
/// </summary>
public record InvitationAcceptedEvent(
    Guid InvitationId,
    Guid OrganizationId,
    string Email,
    Guid AcceptedByUserId,
    Guid RoleId,
    string? AcceptedFromIP,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Invitation declined by invitee.
/// </summary>
public record InvitationDeclinedEvent(
    Guid InvitationId,
    Guid OrganizationId,
    string Email,
    string? Reason,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Invitation revoked by organization.
/// </summary>
public record InvitationRevokedEvent(
    Guid InvitationId,
    Guid OrganizationId,
    string Email,
    Guid RevokedByUserId,
    string Reason,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Invitation expired (auto or manual).
/// </summary>
public record InvitationExpiredEvent(
    Guid InvitationId,
    Guid OrganizationId,
    string Email,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Invitation reminder sent.
/// </summary>
public record InvitationReminderSentEvent(
    Guid InvitationId,
    Guid OrganizationId,
    string Email,
    int ReminderNumber,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Invitation expiration extended.
/// </summary>
public record InvitationExpirationExtendedEvent(
    Guid InvitationId,
    Guid OrganizationId,
    string Email,
    DateTime NewExpiresAt,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Invitation role updated.
/// </summary>
public record InvitationRoleUpdatedEvent(
    Guid InvitationId,
    Guid OrganizationId,
    string Email,
    Guid OldRoleId,
    Guid NewRoleId,
    Guid UpdatedByUserId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Invitation resent with new token.
/// </summary>
public record InvitationResentEvent(
    Guid InvitationId,
    Guid OrganizationId,
    string Email,
    Guid ResentByUserId,
    DateTime NewExpiresAt,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Bulk invitations sent.
/// </summary>
public record BulkInvitationsSentEvent(
    Guid OrganizationId,
    int TotalSent,
    int SuccessCount,
    int FailedCount,
    Guid RoleId,
    Guid SentByUserId,
    DateTime OccurredOn) : IDomainEvent;
