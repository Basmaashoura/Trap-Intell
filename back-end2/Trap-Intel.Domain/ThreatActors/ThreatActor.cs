using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Shared;
using Trap_Intel.Domain.ThreatActors.Enums;
using Trap_Intel.Domain.ThreatActors.Events;
using Trap_Intel.Domain.ThreatActors.ValueObjects;
using Trap_Intel.Domain.ThreatActors.Entities;

namespace Trap_Intel.Domain.ThreatActors;

/// <summary>
/// Represents a threat actor profile built from correlated attacks.
/// Aggregates attack data across honeypots to identify and track attackers.
/// Owns child entities for IPs, TTPs, patterns, and intel notes.
/// </summary>
public class ThreatActor : AggregateRoot<Guid>
{
    private List<ThreatActorIPEntity> _associatedIPs = new();
    private List<Guid> _correlatedAttackIds = new();
    private List<Guid> _targetedHoneypotIds = new();
    private List<ThreatActorTTPEntity> _observedTTPs = new();
    private List<BehaviorPatternEntity> _behaviorPatterns = new();
    private List<ThreatIntelNoteEntity> _intelNotes = new();

    // Private constructor for EF
    private ThreatActor() { }

    private ThreatActor(
        Guid id,
        Guid organizationId,
        string initialIPAddress,
        Guid firstAttackEventId,
        Guid firstHoneypotId)
        : base(id)
    {
        OrganizationId = organizationId;
        Type = ThreatActorType.Unknown;
        ThreatLevel = ThreatLevel.Low;
        Status = ThreatActorStatus.Active;
        Confidence = IdentificationConfidence.Low;
        ThreatScore = 10; // Initial score
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Stats = ThreatActorStats.Initial(DateTime.UtcNow);

        // Add initial IP as entity
        var ipResult = ThreatActorIPEntity.Create(id, initialIPAddress);
        if (ipResult.IsSuccess)
        {
            ipResult.Value.MarkAsPrimary();
            _associatedIPs.Add(ipResult.Value);
        }

        _correlatedAttackIds.Add(firstAttackEventId);
        _targetedHoneypotIds.Add(firstHoneypotId);
    }

    #region Properties

    /// <summary>
    /// Organization that owns this threat actor profile.
    /// </summary>
    public Guid OrganizationId { get; private set; }

    /// <summary>
    /// Display name/alias for this threat actor.
    /// </summary>
    public string? Alias { get; private set; }

    /// <summary>
    /// Classification of threat actor type.
    /// </summary>
    public ThreatActorType Type { get; private set; }

    /// <summary>
    /// Current threat level.
    /// </summary>
    public ThreatLevel ThreatLevel { get; private set; }

    /// <summary>
    /// Status of this threat actor profile.
    /// </summary>
    public ThreatActorStatus Status { get; private set; }

    /// <summary>
    /// Confidence in threat actor identification.
    /// </summary>
    public IdentificationConfidence Confidence { get; private set; }

    /// <summary>
    /// Primary motivation (if known).
    /// </summary>
    public ThreatMotivation Motivation { get; private set; } = ThreatMotivation.Unknown;

    /// <summary>
    /// Primary region of origin.
    /// </summary>
    public ThreatRegion Region { get; private set; } = ThreatRegion.Unknown;

    /// <summary>
    /// Overall threat score (0-100).
    /// </summary>
    public decimal ThreatScore { get; private set; }

    /// <summary>
    /// Detailed threat score breakdown.
    /// </summary>
    public ThreatScoreBreakdown? ScoreBreakdown { get; private set; }

    /// <summary>
    /// Activity statistics.
    /// </summary>
    public ThreatActorStats Stats { get; private set; } = null!;

    /// <summary>
    /// When profile was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When profile was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// When threat actor was last active.
    /// </summary>
    public DateTime LastActiveAt => Stats.LastAttackAt;

    /// <summary>
    /// Associated IP addresses.
    /// </summary>
    public IReadOnlyList<ThreatActorIPEntity> AssociatedIPs => _associatedIPs.AsReadOnly();

    /// <summary>
    /// Correlated attack event IDs.
    /// </summary>
    public IReadOnlyList<Guid> CorrelatedAttackIds => _correlatedAttackIds.AsReadOnly();

    /// <summary>
    /// Targeted honeypot IDs.
    /// </summary>
    public IReadOnlyList<Guid> TargetedHoneypotIds => _targetedHoneypotIds.AsReadOnly();

    /// <summary>
    /// Observed TTPs.
    /// </summary>
    public IReadOnlyList<ThreatActorTTPEntity> ObservedTTPs => _observedTTPs.AsReadOnly();

    /// <summary>
    /// Detected behavior patterns.
    /// </summary>
    public IReadOnlyList<BehaviorPatternEntity> BehaviorPatterns => _behaviorPatterns.AsReadOnly();

    /// <summary>
    /// Intelligence notes.
    /// </summary>
    public IReadOnlyList<ThreatIntelNoteEntity> IntelNotes => _intelNotes.AsReadOnly();

    #endregion

    #region Factory Methods

    /// <summary>
    /// Create new threat actor from first attack event.
    /// </summary>
    public static Result<ThreatActor> Create(
        Guid organizationId,
        string initialIPAddress,
        Guid firstAttackEventId,
        Guid firstHoneypotId,
        string? country = null,
        string? countryCode = null,
        string? city = null,
        string? isp = null,
        string? asn = null)
    {
        // Validation
        if (organizationId == Guid.Empty)
            return Result.Failure<ThreatActor>(ThreatActorErrors.InvalidOrganizationId);

        if (string.IsNullOrWhiteSpace(initialIPAddress))
            return Result.Failure<ThreatActor>(ThreatActorErrors.InvalidIPAddress);

        if (firstAttackEventId == Guid.Empty)
            return Result.Failure<ThreatActor>(ThreatActorErrors.InvalidAttackEventId);

        if (firstHoneypotId == Guid.Empty)
            return Result.Failure<ThreatActor>(ThreatActorErrors.InvalidHoneypotId);

        var threatActor = new ThreatActor(
            Guid.NewGuid(),
            organizationId,
            initialIPAddress,
            firstAttackEventId,
            firstHoneypotId);

        // Update IP with geolocation if provided
        if (!string.IsNullOrWhiteSpace(country) && threatActor._associatedIPs.Count > 0)
        {
            threatActor._associatedIPs[0].UpdateGeolocation(country, countryCode, city, null, isp, asn);
            threatActor.Region = DetermineRegion(country);
        }

        // Raise event
        threatActor.RaiseDomainEvent(new ThreatActorIdentifiedEvent(
            threatActor.Id,
            organizationId,
            initialIPAddress,
            threatActor.Type,
            threatActor.ThreatLevel,
            DateTime.UtcNow));

        return Result.Success(threatActor);
    }

    /// <summary>
    /// Reconstruct from database.
    /// </summary>
    public static ThreatActor Reconstruct(
        Guid id,
        Guid organizationId,
        string? alias,
        ThreatActorType type,
        ThreatLevel threatLevel,
        ThreatActorStatus status,
        IdentificationConfidence confidence,
        ThreatMotivation motivation,
        ThreatRegion region,
        decimal threatScore,
        ThreatScoreBreakdown? scoreBreakdown,
        ThreatActorStats stats,
        DateTime createdAt,
        DateTime updatedAt,
        List<ThreatActorIPEntity>? associatedIPs = null,
        List<Guid>? correlatedAttackIds = null,
        List<Guid>? targetedHoneypotIds = null,
        List<ThreatActorTTPEntity>? observedTTPs = null,
        List<BehaviorPatternEntity>? behaviorPatterns = null,
        List<ThreatIntelNoteEntity>? intelNotes = null)
    {
        return new ThreatActor
        {
            Id = id,
            OrganizationId = organizationId,
            Alias = alias,
            Type = type,
            ThreatLevel = threatLevel,
            Status = status,
            Confidence = confidence,
            Motivation = motivation,
            Region = region,
            ThreatScore = threatScore,
            ScoreBreakdown = scoreBreakdown,
            Stats = stats,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            _associatedIPs = associatedIPs ?? new(),
            _correlatedAttackIds = correlatedAttackIds ?? new(),
            _targetedHoneypotIds = targetedHoneypotIds ?? new(),
            _observedTTPs = observedTTPs ?? new(),
            _behaviorPatterns = behaviorPatterns ?? new(),
            _intelNotes = intelNotes ?? new()
        };
    }

    #endregion

    #region Domain Behaviors

    /// <summary>
    /// Correlate new attack event to this threat actor.
    /// </summary>
    public Result CorrelateAttack(
        Guid attackEventId,
        Guid honeypotId,
        string ipAddress,
        bool hasCredentials = false,
        bool hasMalware = false)
    {
        if (attackEventId == Guid.Empty)
            return Result.Failure(ThreatActorErrors.InvalidAttackEventId);

        if (_correlatedAttackIds.Contains(attackEventId))
            return Result.Failure(ThreatActorErrors.AttackAlreadyCorrelated);

        // Check if new IP
        var existingIP = _associatedIPs.FirstOrDefault(ip => ip.IPAddress == ipAddress);
        bool isNewIP = existingIP == null;

        if (isNewIP && !string.IsNullOrWhiteSpace(ipAddress))
        {
            var ipResult = ThreatActorIPEntity.Create(Id, ipAddress);
            if (ipResult.IsSuccess)
            {
                _associatedIPs.Add(ipResult.Value);

                RaiseDomainEvent(new ThreatActorIPCreatedEvent(
                    ipResult.Value.Id,
                    Id,
                    OrganizationId,
                    ipAddress,
                    null,
                    _associatedIPs.Count,
                    DateTime.UtcNow));
            }
        }
        else if (existingIP != null)
        {
            existingIP.RecordAttack();

            RaiseDomainEvent(new ThreatActorIPAttackRecordedEvent(
                existingIP.Id,
                Id,
                OrganizationId,
                ipAddress,
                existingIP.AttackCount,
                DateTime.UtcNow));
        }

        // Check if new honeypot
        bool isNewHoneypot = !_targetedHoneypotIds.Contains(honeypotId);
        if (isNewHoneypot && honeypotId != Guid.Empty)
        {
            _targetedHoneypotIds.Add(honeypotId);
        }

        // Add attack
        _correlatedAttackIds.Add(attackEventId);

        // Update stats
        Stats = Stats.RecordAttack(isNewIP, isNewHoneypot, hasCredentials, hasMalware);

        // Update confidence based on attack count
        UpdateConfidence();

        // Recalculate threat score
        RecalculateThreatScore();

        // Update primary IP
        UpdatePrimaryIP();

        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new AttackCorrelatedToThreatActorEvent(
            Id,
            attackEventId,
            honeypotId,
            OrganizationId,
            ipAddress,
            Stats.TotalAttacks,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Add observed TTP (MITRE ATT&CK technique).
    /// </summary>
    public Result AddTTP(
        string techniqueId, 
        string techniqueName, 
        string tacticName,
        string? tacticId = null,
        string? subTechniqueId = null,
        string? subTechniqueName = null,
        Guid? attackEventId = null)
    {
        if (string.IsNullOrWhiteSpace(techniqueId))
            return Result.Failure(ThreatActorErrors.InvalidTTP);

        var existingTTP = _observedTTPs.FirstOrDefault(t => t.TechniqueId == techniqueId.ToUpperInvariant());
        if (existingTTP != null)
        {
            existingTTP.RecordUsage(attackEventId);

            RaiseDomainEvent(new ThreatActorTTPUsageRecordedEvent(
                existingTTP.Id,
                Id,
                OrganizationId,
                techniqueId,
                existingTTP.UsageCount,
                attackEventId,
                DateTime.UtcNow));
        }
        else
        {
            var ttpResult = ThreatActorTTPEntity.Create(
                Id, techniqueId, techniqueName, tacticName, tacticId, subTechniqueId, subTechniqueName);

            if (ttpResult.IsFailure)
                return Result.Failure(ttpResult.Errors[0]);

            if (attackEventId.HasValue)
            {
                ttpResult.Value.RecordUsage(attackEventId);
            }

            _observedTTPs.Add(ttpResult.Value);

            RaiseDomainEvent(new ThreatActorTTPCreatedEvent(
                ttpResult.Value.Id,
                Id,
                OrganizationId,
                techniqueId,
                techniqueName,
                tacticName,
                _observedTTPs.Count,
                DateTime.UtcNow));
        }

        UpdatedAt = DateTime.UtcNow;
        RecalculateThreatScore();

        return Result.Success();
    }

    /// <summary>
    /// Add behavior pattern.
    /// </summary>
    public Result AddBehaviorPattern(
        string category, 
        string description,
        BehaviorPatternType? patternType = null,
        bool detectedByAI = false,
        Guid? attackEventId = null)
    {
        if (string.IsNullOrWhiteSpace(category))
            return Result.Failure(ThreatActorErrors.InvalidPatternCategory);

        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure(ThreatActorErrors.InvalidPatternDescription);

        var existingPattern = _behaviorPatterns.FirstOrDefault(p => 
            p.Category == category && p.Description == description);

        if (existingPattern != null)
        {
            existingPattern.RecordOccurrence(attackEventId);

            RaiseDomainEvent(new BehaviorPatternOccurrenceRecordedEvent(
                existingPattern.Id,
                Id,
                OrganizationId,
                category,
                existingPattern.Occurrences,
                attackEventId,
                DateTime.UtcNow));
        }
        else
        {
            var patternResult = BehaviorPatternEntity.Create(
                Id, category, description, patternType, detectedByAI);

            if (patternResult.IsFailure)
                return Result.Failure(patternResult.Errors[0]);

            if (attackEventId.HasValue)
            {
                patternResult.Value.RecordOccurrence(attackEventId);
            }

            _behaviorPatterns.Add(patternResult.Value);

            RaiseDomainEvent(new BehaviorPatternCreatedEvent(
                patternResult.Value.Id,
                Id,
                OrganizationId,
                category,
                description,
                patternResult.Value.PatternType,
                detectedByAI,
                DateTime.UtcNow));
        }

        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Escalate threat level.
    /// </summary>
    public Result EscalateThreatLevel(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(ThreatActorErrors.InvalidReason);

        if (ThreatLevel == ThreatLevel.Severe)
            return Result.Failure(ThreatActorErrors.CannotEscalateHigher);

        var oldLevel = ThreatLevel;
        ThreatLevel = (ThreatLevel)((int)ThreatLevel + 1);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ThreatLevelEscalatedEvent(
            Id,
            OrganizationId,
            oldLevel,
            ThreatLevel,
            reason,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// De-escalate threat level.
    /// </summary>
    public Result DeescalateThreatLevel(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(ThreatActorErrors.InvalidReason);

        if (ThreatLevel == ThreatLevel.Unknown || ThreatLevel == ThreatLevel.Low)
            return Result.Failure(ThreatActorErrors.CannotDeescalateLower);

        var oldLevel = ThreatLevel;
        ThreatLevel = (ThreatLevel)((int)ThreatLevel - 1);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ThreatLevelDeescalatedEvent(
            Id,
            OrganizationId,
            oldLevel,
            ThreatLevel,
            reason,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Block IP address.
    /// </summary>
    public Result BlockIP(string ipAddress, Guid blockedByUserId, string reason)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return Result.Failure(ThreatActorErrors.InvalidIPAddress);

        if (blockedByUserId == Guid.Empty)
            return Result.Failure(ThreatActorErrors.InvalidUserId);

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(ThreatActorErrors.InvalidReason);

        var ip = _associatedIPs.FirstOrDefault(i => i.IPAddress == ipAddress);
        if (ip == null)
            return Result.Failure(ThreatActorErrors.IPNotFound);

        var result = ip.Block(blockedByUserId, reason);
        if (result.IsFailure)
            return result;

        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ThreatActorIPBlockedByUserEvent(
            ip.Id,
            Id,
            OrganizationId,
            ipAddress,
            blockedByUserId,
            reason,
            DateTime.UtcNow));

        // Check if all IPs are blocked
        if (_associatedIPs.All(i => i.IsBlocked))
        {
            Status = ThreatActorStatus.Blocked;

            RaiseDomainEvent(new AllThreatActorIPsBlockedEvent(
                Id,
                OrganizationId,
                _associatedIPs.Count,
                DateTime.UtcNow));

            RaiseDomainEvent(new ThreatActorStatusChangedEvent(
                Id,
                OrganizationId,
                ThreatActorStatus.Active,
                ThreatActorStatus.Blocked,
                "All IPs blocked",
                DateTime.UtcNow));
        }

        return Result.Success();
    }

    /// <summary>
    /// Unblock IP address.
    /// </summary>
    public Result UnblockIP(string ipAddress, Guid unblockedByUserId, string reason)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return Result.Failure(ThreatActorErrors.InvalidIPAddress);

        if (unblockedByUserId == Guid.Empty)
            return Result.Failure(ThreatActorErrors.InvalidUserId);

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(ThreatActorErrors.InvalidReason);

        var ip = _associatedIPs.FirstOrDefault(i => i.IPAddress == ipAddress);
        if (ip == null)
            return Result.Failure(ThreatActorErrors.IPNotFound);

        var blockedDuration = ip.GetBlockedDuration();
        var result = ip.Unblock(unblockedByUserId, reason);
        if (result.IsFailure)
            return result;

        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ThreatActorIPUnblockedEvent(
            ip.Id,
            Id,
            OrganizationId,
            ipAddress,
            unblockedByUserId,
            reason,
            blockedDuration ?? TimeSpan.Zero,
            DateTime.UtcNow));

        // If threat actor was blocked, reactivate
        if (Status == ThreatActorStatus.Blocked)
        {
            Status = ThreatActorStatus.Active;

            RaiseDomainEvent(new ThreatActorStatusChangedEvent(
                Id,
                OrganizationId,
                ThreatActorStatus.Blocked,
                ThreatActorStatus.Active,
                "IP unblocked",
                DateTime.UtcNow));
        }

        return Result.Success();
    }

    /// <summary>
    /// Change classification.
    /// </summary>
    public Result ChangeClassification(ThreatActorType newType, Guid changedByUserId)
    {
        if (changedByUserId == Guid.Empty)
            return Result.Failure(ThreatActorErrors.InvalidUserId);

        var oldType = Type;
        Type = newType;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ThreatActorClassificationChangedEvent(
            Id,
            OrganizationId,
            oldType,
            newType,
            changedByUserId,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Add intelligence note.
    /// </summary>
    public Result AddIntelNote(
        string content, 
        string source, 
        Guid userId,
        IntelNoteType? noteType = null,
        bool isInternal = true)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Result.Failure(ThreatActorErrors.InvalidNote);

        if (userId == Guid.Empty)
            return Result.Failure(ThreatActorErrors.InvalidUserId);

        var noteResult = ThreatIntelNoteEntity.Create(Id, content, source, userId, noteType, isInternal);
        if (noteResult.IsFailure)
            return Result.Failure(noteResult.Errors[0]);

        _intelNotes.Add(noteResult.Value);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ThreatIntelNoteCreatedEvent(
            noteResult.Value.Id,
            Id,
            OrganizationId,
            userId,
            noteResult.Value.NoteType,
            source,
            isInternal,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Edit intelligence note.
    /// </summary>
    public Result EditIntelNote(Guid noteId, string newContent, Guid editorUserId)
    {
        var note = _intelNotes.FirstOrDefault(n => n.Id == noteId);
        if (note == null)
            return Result.Failure(ThreatActorErrors.NoteNotFound);

        var result = note.Edit(newContent, editorUserId);
        if (result.IsFailure)
            return result;

        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ThreatIntelNoteEditedEvent(
            noteId,
            Id,
            OrganizationId,
            editorUserId,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Delete intelligence note.
    /// </summary>
    public Result DeleteIntelNote(Guid noteId, Guid deleterUserId)
    {
        var note = _intelNotes.FirstOrDefault(n => n.Id == noteId);
        if (note == null)
            return Result.Failure(ThreatActorErrors.NoteNotFound);

        var result = note.Delete(deleterUserId);
        if (result.IsFailure)
            return result;

        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ThreatIntelNoteDeletedEvent(
            noteId,
            Id,
            OrganizationId,
            deleterUserId,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Mark as false positive.
    /// </summary>
    public Result MarkAsFalsePositive(string reason, Guid markedByUserId)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(ThreatActorErrors.InvalidReason);

        if (markedByUserId == Guid.Empty)
            return Result.Failure(ThreatActorErrors.InvalidUserId);

        if (Status == ThreatActorStatus.FalsePositive)
            return Result.Failure(ThreatActorErrors.AlreadyFalsePositive);

        Status = ThreatActorStatus.FalsePositive;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ThreatActorMarkedFalsePositiveEvent(
            Id,
            OrganizationId,
            reason,
            markedByUserId,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Set alias/name for this threat actor.
    /// </summary>
    public Result SetAlias(string alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
            return Result.Failure(ThreatActorErrors.InvalidAlias);

        if (alias.Length > 100)
            return Result.Failure(ThreatActorErrors.AliasTooLong);

        Alias = alias.Trim();
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Clear alias.
    /// </summary>
    public void ClearAlias()
    {
        Alias = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Set motivation for this threat actor.
    /// </summary>
    public Result SetMotivation(ThreatMotivation motivation)
    {
        if (motivation == ThreatMotivation.Unknown)
            return Result.Failure(ThreatActorErrors.InvalidMotivation);

        Motivation = motivation;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Clear motivation (reset to Unknown).
    /// </summary>
    public void ClearMotivation()
    {
        Motivation = ThreatMotivation.Unknown;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update IP geolocation.
    /// </summary>
    public Result UpdateIPGeolocation(
        string ipAddress,
        string? country,
        string? countryCode,
        string? city,
        string? region,
        string? isp,
        string? asn)
    {
        var ip = _associatedIPs.FirstOrDefault(i => i.IPAddress == ipAddress);
        if (ip == null)
            return Result.Failure(ThreatActorErrors.IPNotFound);

        var result = ip.UpdateGeolocation(country, countryCode, city, region, isp, asn);
        if (result.IsFailure)
            return result;

        // Update region if this is the primary IP
        if (ip.IsPrimary && !string.IsNullOrEmpty(country))
        {
            Region = DetermineRegion(country);
        }

        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ThreatActorIPGeolocationUpdatedEvent(
            ip.Id,
            Id,
            OrganizationId,
            ipAddress,
            country,
            city,
            isp,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Mark TTP as signature technique.
    /// </summary>
    public Result MarkTTPAsSignature(Guid ttpId)
    {
        var ttp = _observedTTPs.FirstOrDefault(t => t.Id == ttpId);
        if (ttp == null)
            return Result.Failure(ThreatActorErrors.TTPNotFound);

        ttp.MarkAsSignature();
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ThreatActorTTPMarkedAsSignatureEvent(
            ttpId,
            Id,
            OrganizationId,
            ttp.TechniqueId,
            ttp.TechniqueName,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Mark behavior pattern as distinctive.
    /// </summary>
    public Result MarkPatternAsDistinctive(Guid patternId)
    {
        var pattern = _behaviorPatterns.FirstOrDefault(p => p.Id == patternId);
        if (pattern == null)
            return Result.Failure(ThreatActorErrors.PatternNotFound);

        pattern.MarkAsDistinctive();
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new BehaviorPatternMarkedDistinctiveEvent(
            patternId,
            Id,
            OrganizationId,
            pattern.Category,
            pattern.Description,
            DateTime.UtcNow));

        return Result.Success();
    }

    #endregion

    #region Query Helpers

    /// <summary>
    /// Check if threat actor is active.
    /// </summary>
    public bool IsActive() => Status == ThreatActorStatus.Active || Status == ThreatActorStatus.Monitored;

    /// <summary>
    /// Check if high threat.
    /// </summary>
    public bool IsHighThreat() => ThreatLevel >= ThreatLevel.High;

    /// <summary>
    /// Get days since last activity.
    /// </summary>
    public int GetDaysSinceLastActivity() => (DateTime.UtcNow - Stats.LastAttackAt).Days;

    /// <summary>
    /// Get primary IP (most attacks).
    /// </summary>
    public string? GetPrimaryIP() => _associatedIPs
        .Where(ip => !ip.IsBlocked)
        .OrderByDescending(ip => ip.AttackCount)
        .FirstOrDefault()?.IPAddress;

    /// <summary>
    /// Get IP entity by address.
    /// </summary>
    public ThreatActorIPEntity? GetIPByAddress(string ipAddress) =>
        _associatedIPs.FirstOrDefault(ip => ip.IPAddress == ipAddress);

    /// <summary>
    /// Get TTP entity by technique ID.
    /// </summary>
    public ThreatActorTTPEntity? GetTTPByTechniqueId(string techniqueId) =>
        _observedTTPs.FirstOrDefault(t => t.TechniqueId == techniqueId.ToUpperInvariant());

    /// <summary>
    /// Get intel note by ID.
    /// </summary>
    public ThreatIntelNoteEntity? GetNoteById(Guid noteId) =>
        _intelNotes.FirstOrDefault(n => n.Id == noteId);

    /// <summary>
    /// Get pattern by ID.
    /// </summary>
    public BehaviorPatternEntity? GetPatternById(Guid patternId) =>
        _behaviorPatterns.FirstOrDefault(p => p.Id == patternId);

    /// <summary>
    /// Get all blocked IPs.
    /// </summary>
    public IEnumerable<ThreatActorIPEntity> GetBlockedIPs() =>
        _associatedIPs.Where(ip => ip.IsBlocked);

    /// <summary>
    /// Get all signature TTPs.
    /// </summary>
    public IEnumerable<ThreatActorTTPEntity> GetSignatureTTPs() =>
        _observedTTPs.Where(t => t.IsSignatureTTP);

    /// <summary>
    /// Get all distinctive patterns.
    /// </summary>
    public IEnumerable<BehaviorPatternEntity> GetDistinctivePatterns() =>
        _behaviorPatterns.Where(p => p.IsDistinctive);

    /// <summary>
    /// Get visible notes (not deleted).
    /// </summary>
    public IEnumerable<ThreatIntelNoteEntity> GetVisibleNotes() =>
        _intelNotes.Where(n => !n.IsDeleted);

    /// <summary>
    /// Get pinned notes.
    /// </summary>
    public IEnumerable<ThreatIntelNoteEntity> GetPinnedNotes() =>
        _intelNotes.Where(n => !n.IsDeleted && n.IsPinned);

    /// <summary>
    /// Get high risk IPs.
    /// </summary>
    public IEnumerable<ThreatActorIPEntity> GetHighRiskIPs() =>
        _associatedIPs.Where(ip => ip.IsHighRisk());

    /// <summary>
    /// Get recently active IPs.
    /// </summary>
    public IEnumerable<ThreatActorIPEntity> GetRecentlyActiveIPs(int days = 7) =>
        _associatedIPs.Where(ip => ip.IsRecentlyActive(days));

    /// <summary>
    /// Get total blocked IP count.
    /// </summary>
    public int GetBlockedIPCount() => _associatedIPs.Count(ip => ip.IsBlocked);

    /// <summary>
    /// Get total TTP count.
    /// </summary>
    public int GetTTPCount() => _observedTTPs.Count;

    /// <summary>
    /// Get total pattern count.
    /// </summary>
    public int GetPatternCount() => _behaviorPatterns.Count;

    /// <summary>
    /// Get total note count (visible only).
    /// </summary>
    public int GetNoteCount() => _intelNotes.Count(n => !n.IsDeleted);

    #endregion

    #region Private Methods

    private void UpdateConfidence()
    {
        Confidence = Stats.TotalAttacks switch
        {
            >= 20 => IdentificationConfidence.Confirmed,
            >= 10 => IdentificationConfidence.High,
            >= 5 => IdentificationConfidence.Medium,
            _ => IdentificationConfidence.Low
        };
    }

    private void RecalculateThreatScore()
    {
        var oldScore = ThreatScore;

        ScoreBreakdown = ThreatScoreBreakdown.Calculate(
            Stats.TotalAttacks,
            Stats.CredentialsAttempted + Stats.MalwareUploads, // High severity proxy
            _observedTTPs.Count,
            Stats.LastAttackAt);

        ThreatScore = ScoreBreakdown.TotalScore;

        if (Math.Abs(oldScore - ThreatScore) > 5) // Only raise if significant change
        {
            RaiseDomainEvent(new ThreatScoreRecalculatedEvent(
                Id,
                OrganizationId,
                oldScore,
                ThreatScore,
                DateTime.UtcNow));
        }
    }

    private void UpdatePrimaryIP()
    {
        // Find current primary IP
        var currentPrimary = _associatedIPs.FirstOrDefault(ip => ip.IsPrimary);
        
        // Find IP with most attacks
        var topIP = _associatedIPs
            .Where(ip => !ip.IsBlocked)
            .OrderByDescending(ip => ip.AttackCount)
            .FirstOrDefault();

        if (topIP != null && (currentPrimary == null || currentPrimary.Id != topIP.Id))
        {
            // Unmark old primary
            if (currentPrimary != null)
            {
                currentPrimary.UnmarkAsPrimary();
            }

            // Mark new primary
            topIP.MarkAsPrimary();

            RaiseDomainEvent(new ThreatActorPrimaryIPChangedEvent(
                Id,
                OrganizationId,
                currentPrimary?.IPAddress,
                topIP.IPAddress,
                DateTime.UtcNow));

            // Update region based on new primary
            if (!string.IsNullOrEmpty(topIP.Country))
            {
                Region = DetermineRegion(topIP.Country);
            }
        }
    }

    private static ThreatRegion DetermineRegion(string country)
    {
        return country?.ToUpper() switch
        {
            "US" or "CA" or "MX" => ThreatRegion.NorthAmerica,
            "BR" or "AR" or "CL" or "CO" => ThreatRegion.SouthAmerica,
            "GB" or "DE" or "FR" or "IT" or "ES" or "NL" => ThreatRegion.WesternEurope,
            "PL" or "CZ" or "HU" or "RO" or "BG" or "UA" => ThreatRegion.EasternEurope,
            "RU" => ThreatRegion.Russia,
            "IR" or "SA" or "AE" or "IL" or "TR" => ThreatRegion.MiddleEast,
            "CN" or "JP" or "KR" or "TW" or "HK" => ThreatRegion.EastAsia,
            "IN" or "PK" or "BD" => ThreatRegion.SouthAsia,
            "TH" or "VN" or "ID" or "MY" or "SG" or "PH" => ThreatRegion.SoutheastAsia,
            "AU" or "NZ" => ThreatRegion.Oceania,
            _ => ThreatRegion.Unknown
        };
    }

    #endregion
}
