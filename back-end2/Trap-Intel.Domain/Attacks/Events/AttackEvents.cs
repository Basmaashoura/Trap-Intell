using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Attacks.Enums;

namespace Trap_Intel.Domain.Attacks.Events;

/// <summary>
/// Attack event received from Go honeypot
/// </summary>
public record AttackEventReceivedEvent(
    Guid AttackEventId,
    Guid HoneypotId,
    Guid OrganizationId,
    string AttackType,
    string Severity,
    string SourceIP,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Attack event analyzed by AI
/// </summary>
public record AttackEventAnalyzedEvent(
    Guid AttackEventId,
    Guid HoneypotId,
    Guid OrganizationId,
    decimal ThreatScore,
    AttackIntent Intent,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// High-severity attack detected (triggers alert)
/// </summary>
public record HighSeverityAttackDetectedEvent(
    Guid AttackEventId,
    Guid HoneypotId,
    Guid OrganizationId,
    string SourceIP,
    AttackSeverity Severity,
    decimal ThreatScore,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Malware uploaded to honeypot
/// </summary>
public record MalwareUploadedEvent(
    Guid AttackEventId,
    Guid HoneypotId,
    Guid OrganizationId,
    string SourceIP,
    string FileHash,
    long FileSize,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Attack event linked to threat actor
/// </summary>
public record AttackEventLinkedToThreatActorEvent(
    Guid AttackEventId,
    Guid ThreatActorId,
    Guid HoneypotId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Attack event marked as anomaly
/// </summary>
public record AttackEventMarkedAsAnomalyEvent(
    Guid AttackEventId,
    Guid HoneypotId,
    Guid OrganizationId,
    string SourceIP,
    AttackType AttackType,
    DateTime OccurredOn) : IDomainEvent;
