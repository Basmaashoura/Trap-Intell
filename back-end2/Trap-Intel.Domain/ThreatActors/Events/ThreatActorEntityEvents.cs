using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.ThreatActors.Entities;
using Trap_Intel.Domain.ThreatActors.Enums;

namespace Trap_Intel.Domain.ThreatActors.Events;

/// <summary>
/// Events related to ThreatActor child entities.
/// </summary>

#region IP Entity Events

/// <summary>
/// IP entity created for threat actor.
/// </summary>
public record ThreatActorIPCreatedEvent(
    Guid IPEntityId,
    Guid ThreatActorId,
    Guid OrganizationId,
    string IPAddress,
    string? Country,
    int TotalIPs,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// IP attack recorded.
/// </summary>
public record ThreatActorIPAttackRecordedEvent(
    Guid IPEntityId,
    Guid ThreatActorId,
    Guid OrganizationId,
    string IPAddress,
    int NewAttackCount,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// IP geolocation updated.
/// </summary>
public record ThreatActorIPGeolocationUpdatedEvent(
    Guid IPEntityId,
    Guid ThreatActorId,
    Guid OrganizationId,
    string IPAddress,
    string? Country,
    string? City,
    string? ISP,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// IP blocked by user.
/// </summary>
public record ThreatActorIPBlockedByUserEvent(
    Guid IPEntityId,
    Guid ThreatActorId,
    Guid OrganizationId,
    string IPAddress,
    Guid BlockedByUserId,
    string BlockReason,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// IP unblocked by user.
/// </summary>
public record ThreatActorIPUnblockedEvent(
    Guid IPEntityId,
    Guid ThreatActorId,
    Guid OrganizationId,
    string IPAddress,
    Guid UnblockedByUserId,
    string UnblockReason,
    TimeSpan BlockedDuration,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// IP reputation score changed.
/// </summary>
public record ThreatActorIPReputationChangedEvent(
    Guid IPEntityId,
    Guid ThreatActorId,
    Guid OrganizationId,
    string IPAddress,
    int OldScore,
    int NewScore,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Primary IP changed for threat actor.
/// </summary>
public record ThreatActorPrimaryIPChangedEvent(
    Guid ThreatActorId,
    Guid OrganizationId,
    string? OldPrimaryIP,
    string NewPrimaryIP,
    DateTime OccurredOn) : IDomainEvent;

#endregion

#region TTP Entity Events

/// <summary>
/// TTP entity created for threat actor.
/// </summary>
public record ThreatActorTTPCreatedEvent(
    Guid TTPEntityId,
    Guid ThreatActorId,
    Guid OrganizationId,
    string TechniqueId,
    string TechniqueName,
    string TacticName,
    int TotalTTPs,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// TTP usage recorded.
/// </summary>
public record ThreatActorTTPUsageRecordedEvent(
    Guid TTPEntityId,
    Guid ThreatActorId,
    Guid OrganizationId,
    string TechniqueId,
    int NewUsageCount,
    Guid? AttackEventId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// TTP confidence updated.
/// </summary>
public record ThreatActorTTPConfidenceUpdatedEvent(
    Guid TTPEntityId,
    Guid ThreatActorId,
    Guid OrganizationId,
    string TechniqueId,
    int OldConfidence,
    int NewConfidence,
    TTPDetectionMethod DetectionMethod,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// TTP marked as signature for threat actor.
/// </summary>
public record ThreatActorTTPMarkedAsSignatureEvent(
    Guid TTPEntityId,
    Guid ThreatActorId,
    Guid OrganizationId,
    string TechniqueId,
    string TechniqueName,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// TTP severity changed.
/// </summary>
public record ThreatActorTTPSeverityChangedEvent(
    Guid TTPEntityId,
    Guid ThreatActorId,
    Guid OrganizationId,
    string TechniqueId,
    TTPSeverity OldSeverity,
    TTPSeverity NewSeverity,
    DateTime OccurredOn) : IDomainEvent;

#endregion

#region Behavior Pattern Events

/// <summary>
/// Behavior pattern entity created.
/// </summary>
public record BehaviorPatternCreatedEvent(
    Guid PatternEntityId,
    Guid ThreatActorId,
    Guid OrganizationId,
    string Category,
    string Description,
    BehaviorPatternType PatternType,
    bool DetectedByAI,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Behavior pattern occurrence recorded.
/// </summary>
public record BehaviorPatternOccurrenceRecordedEvent(
    Guid PatternEntityId,
    Guid ThreatActorId,
    Guid OrganizationId,
    string Category,
    int NewOccurrenceCount,
    Guid? AttackEventId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Behavior pattern marked as distinctive.
/// </summary>
public record BehaviorPatternMarkedDistinctiveEvent(
    Guid PatternEntityId,
    Guid ThreatActorId,
    Guid OrganizationId,
    string Category,
    string Description,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Behavior pattern severity changed.
/// </summary>
public record BehaviorPatternSeverityChangedEvent(
    Guid PatternEntityId,
    Guid ThreatActorId,
    Guid OrganizationId,
    PatternSeverity OldSeverity,
    PatternSeverity NewSeverity,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Behavior pattern confidence updated.
/// </summary>
public record BehaviorPatternConfidenceUpdatedEvent(
    Guid PatternEntityId,
    Guid ThreatActorId,
    Guid OrganizationId,
    int OldConfidence,
    int NewConfidence,
    DateTime OccurredOn) : IDomainEvent;

#endregion

#region Intel Note Events

/// <summary>
/// Intel note created.
/// </summary>
public record ThreatIntelNoteCreatedEvent(
    Guid NoteEntityId,
    Guid ThreatActorId,
    Guid OrganizationId,
    Guid AuthorUserId,
    IntelNoteType NoteType,
    string Source,
    bool IsInternal,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Intel note edited.
/// </summary>
public record ThreatIntelNoteEditedEvent(
    Guid NoteEntityId,
    Guid ThreatActorId,
    Guid OrganizationId,
    Guid EditedByUserId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Intel note deleted.
/// </summary>
public record ThreatIntelNoteDeletedEvent(
    Guid NoteEntityId,
    Guid ThreatActorId,
    Guid OrganizationId,
    Guid DeletedByUserId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Intel note restored.
/// </summary>
public record ThreatIntelNoteRestoredEvent(
    Guid NoteEntityId,
    Guid ThreatActorId,
    Guid OrganizationId,
    Guid RestoredByUserId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Intel note pinned.
/// </summary>
public record ThreatIntelNotePinnedEvent(
    Guid NoteEntityId,
    Guid ThreatActorId,
    Guid OrganizationId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Intel note visibility changed.
/// </summary>
public record ThreatIntelNoteVisibilityChangedEvent(
    Guid NoteEntityId,
    Guid ThreatActorId,
    Guid OrganizationId,
    bool IsNowInternal,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Intel note confidence level changed.
/// </summary>
public record ThreatIntelNoteConfidenceChangedEvent(
    Guid NoteEntityId,
    Guid ThreatActorId,
    Guid OrganizationId,
    IntelConfidenceLevel OldLevel,
    IntelConfidenceLevel NewLevel,
    DateTime OccurredOn) : IDomainEvent;

#endregion

#region Aggregate Summary Events

/// <summary>
/// All IPs blocked for threat actor.
/// </summary>
public record AllThreatActorIPsBlockedEvent(
    Guid ThreatActorId,
    Guid OrganizationId,
    int TotalIPsBlocked,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Threat actor profile enriched with new data.
/// </summary>
public record ThreatActorProfileEnrichedEvent(
    Guid ThreatActorId,
    Guid OrganizationId,
    int TotalIPs,
    int TotalTTPs,
    int TotalPatterns,
    int TotalNotes,
    decimal ThreatScore,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Signature TTP identified for threat actor.
/// </summary>
public record ThreatActorSignatureIdentifiedEvent(
    Guid ThreatActorId,
    Guid OrganizationId,
    string TechniqueId,
    string TechniqueName,
    int ConfidenceScore,
    DateTime OccurredOn) : IDomainEvent;

#endregion
