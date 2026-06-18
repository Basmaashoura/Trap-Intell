using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.ThreatActors.Enums;

namespace Trap_Intel.Domain.ThreatActors.Events;

/// <summary>
/// New threat actor identified.
/// </summary>
public record ThreatActorIdentifiedEvent(
    Guid ThreatActorId,
    Guid OrganizationId,
    string InitialIPAddress,
    ThreatActorType Type,
    ThreatLevel ThreatLevel,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Attack correlated to existing threat actor.
/// </summary>
public record AttackCorrelatedToThreatActorEvent(
    Guid ThreatActorId,
    Guid AttackEventId,
    Guid HoneypotId,
    Guid OrganizationId,
    string IPAddress,
    int TotalAttacks,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Threat level escalated.
/// </summary>
public record ThreatLevelEscalatedEvent(
    Guid ThreatActorId,
    Guid OrganizationId,
    ThreatLevel OldLevel,
    ThreatLevel NewLevel,
    string Reason,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Threat level de-escalated.
/// </summary>
public record ThreatLevelDeescalatedEvent(
    Guid ThreatActorId,
    Guid OrganizationId,
    ThreatLevel OldLevel,
    ThreatLevel NewLevel,
    string Reason,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// New IP associated with threat actor.
/// </summary>
public record ThreatActorIPAddedEvent(
    Guid ThreatActorId,
    Guid OrganizationId,
    string IPAddress,
    int TotalIPs,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Threat actor IP blocked.
/// </summary>
public record ThreatActorIPBlockedEvent(
    Guid ThreatActorId,
    Guid OrganizationId,
    string IPAddress,
    Guid BlockedByUserId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// New TTP observed for threat actor.
/// </summary>
public record ThreatActorTTPObservedEvent(
    Guid ThreatActorId,
    Guid OrganizationId,
    string TechniqueId,
    string TechniqueName,
    int TotalTTPs,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Behavior pattern detected.
/// </summary>
public record BehaviorPatternDetectedEvent(
    Guid ThreatActorId,
    Guid OrganizationId,
    string PatternCategory,
    string PatternDescription,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Threat actor classification changed.
/// </summary>
public record ThreatActorClassificationChangedEvent(
    Guid ThreatActorId,
    Guid OrganizationId,
    ThreatActorType OldType,
    ThreatActorType NewType,
    Guid ChangedByUserId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Threat actor status changed.
/// </summary>
public record ThreatActorStatusChangedEvent(
    Guid ThreatActorId,
    Guid OrganizationId,
    ThreatActorStatus OldStatus,
    ThreatActorStatus NewStatus,
    string? Reason,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Intelligence note added.
/// </summary>
public record ThreatIntelNoteAddedEvent(
    Guid ThreatActorId,
    Guid OrganizationId,
    string NoteContent,
    Guid AddedByUserId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Threat score recalculated.
/// </summary>
public record ThreatScoreRecalculatedEvent(
    Guid ThreatActorId,
    Guid OrganizationId,
    decimal OldScore,
    decimal NewScore,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Threat actor merged with another.
/// </summary>
public record ThreatActorMergedEvent(
    Guid PrimaryThreatActorId,
    Guid MergedThreatActorId,
    Guid OrganizationId,
    Guid MergedByUserId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Threat actor marked as false positive.
/// </summary>
public record ThreatActorMarkedFalsePositiveEvent(
    Guid ThreatActorId,
    Guid OrganizationId,
    string Reason,
    Guid MarkedByUserId,
    DateTime OccurredOn) : IDomainEvent;
