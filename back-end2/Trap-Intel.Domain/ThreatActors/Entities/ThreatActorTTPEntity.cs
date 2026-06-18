using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Domain.ThreatActors.Entities;

/// <summary>
/// Represents a MITRE ATT&CK TTP observed from a threat actor.
/// Child entity owned by ThreatActor aggregate.
/// Tracks technique usage patterns and confidence.
/// </summary>
public class ThreatActorTTPEntity : Entity<Guid>
{
    private List<Guid> _observedInAttackIds = new();

    // Private constructor for EF
    private ThreatActorTTPEntity() { }

    private ThreatActorTTPEntity(
        Guid id,
        Guid threatActorId,
        string techniqueId,
        string techniqueName,
        string tacticName)
        : base(id)
    {
        ThreatActorId = threatActorId;
        TechniqueId = techniqueId;
        TechniqueName = techniqueName;
        TacticName = tacticName;
        UsageCount = 1;
        FirstUsedAt = DateTime.UtcNow;
        LastUsedAt = DateTime.UtcNow;
        ConfidenceScore = 50; // Default medium confidence
    }

    #region Properties

    /// <summary>
    /// Parent threat actor ID.
    /// </summary>
    public Guid ThreatActorId { get; private set; }

    /// <summary>
    /// MITRE ATT&CK technique ID (e.g., T1110).
    /// </summary>
    public string TechniqueId { get; private set; } = string.Empty;

    /// <summary>
    /// Human-readable technique name.
    /// </summary>
    public string TechniqueName { get; private set; } = string.Empty;

    /// <summary>
    /// Sub-technique ID if applicable (e.g., T1110.001).
    /// </summary>
    public string? SubTechniqueId { get; private set; }

    /// <summary>
    /// Sub-technique name if applicable.
    /// </summary>
    public string? SubTechniqueName { get; private set; }

    /// <summary>
    /// MITRE ATT&CK tactic ID (e.g., TA0006).
    /// </summary>
    public string? TacticId { get; private set; }

    /// <summary>
    /// Tactic name (e.g., Credential Access).
    /// </summary>
    public string TacticName { get; private set; } = string.Empty;

    /// <summary>
    /// Number of times this TTP was observed.
    /// </summary>
    public int UsageCount { get; private set; }

    /// <summary>
    /// When this TTP was first observed.
    /// </summary>
    public DateTime FirstUsedAt { get; private set; }

    /// <summary>
    /// When this TTP was most recently observed.
    /// </summary>
    public DateTime LastUsedAt { get; private set; }

    /// <summary>
    /// Confidence score in TTP identification (0-100).
    /// </summary>
    public int ConfidenceScore { get; private set; }

    /// <summary>
    /// How the TTP was detected.
    /// </summary>
    public TTPDetectionMethod DetectionMethod { get; private set; } = TTPDetectionMethod.Automatic;

    /// <summary>
    /// Severity/impact of this TTP.
    /// </summary>
    public TTPSeverity Severity { get; private set; } = TTPSeverity.Medium;

    /// <summary>
    /// Whether this is a signature TTP for this actor.
    /// </summary>
    public bool IsSignatureTTP { get; private set; }

    /// <summary>
    /// Attack event IDs where this TTP was observed.
    /// </summary>
    public IReadOnlyList<Guid> ObservedInAttackIds => _observedInAttackIds.AsReadOnly();

    /// <summary>
    /// Additional notes about the TTP usage.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// URL to MITRE ATT&CK reference.
    /// </summary>
    public string? MitreUrl { get; private set; }

    /// <summary>
    /// Additional metadata as JSON.
    /// </summary>
    public string? Metadata { get; private set; }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Create a new TTP entity.
    /// </summary>
    public static Result<ThreatActorTTPEntity> Create(
        Guid threatActorId,
        string techniqueId,
        string techniqueName,
        string tacticName,
        string? tacticId = null,
        string? subTechniqueId = null,
        string? subTechniqueName = null)
    {
        if (threatActorId == Guid.Empty)
            return Result.Failure<ThreatActorTTPEntity>(ThreatActorErrors.InvalidThreatActorId);

        if (string.IsNullOrWhiteSpace(techniqueId))
            return Result.Failure<ThreatActorTTPEntity>(ThreatActorErrors.InvalidTTP);

        if (string.IsNullOrWhiteSpace(techniqueName))
            return Result.Failure<ThreatActorTTPEntity>(ThreatActorErrors.InvalidTTP);

        if (string.IsNullOrWhiteSpace(tacticName))
            return Result.Failure<ThreatActorTTPEntity>(ThreatActorErrors.InvalidTTP);

        var entity = new ThreatActorTTPEntity(
            Guid.NewGuid(),
            threatActorId,
            techniqueId.Trim().ToUpperInvariant(),
            techniqueName.Trim(),
            tacticName.Trim())
        {
            TacticId = tacticId?.Trim().ToUpperInvariant(),
            SubTechniqueId = subTechniqueId?.Trim().ToUpperInvariant(),
            SubTechniqueName = subTechniqueName?.Trim(),
            MitreUrl = GenerateMitreUrl(techniqueId, subTechniqueId)
        };

        return Result.Success(entity);
    }

    /// <summary>
    /// Reconstruct from database.
    /// </summary>
    public static ThreatActorTTPEntity Reconstruct(
        Guid id,
        Guid threatActorId,
        string techniqueId,
        string techniqueName,
        string? subTechniqueId,
        string? subTechniqueName,
        string? tacticId,
        string tacticName,
        int usageCount,
        DateTime firstUsedAt,
        DateTime lastUsedAt,
        int confidenceScore,
        TTPDetectionMethod detectionMethod,
        TTPSeverity severity,
        bool isSignatureTTP,
        List<Guid>? observedInAttackIds,
        string? notes,
        string? mitreUrl,
        string? metadata)
    {
        return new ThreatActorTTPEntity
        {
            Id = id,
            ThreatActorId = threatActorId,
            TechniqueId = techniqueId,
            TechniqueName = techniqueName,
            SubTechniqueId = subTechniqueId,
            SubTechniqueName = subTechniqueName,
            TacticId = tacticId,
            TacticName = tacticName,
            UsageCount = usageCount,
            FirstUsedAt = firstUsedAt,
            LastUsedAt = lastUsedAt,
            ConfidenceScore = confidenceScore,
            DetectionMethod = detectionMethod,
            Severity = severity,
            IsSignatureTTP = isSignatureTTP,
            _observedInAttackIds = observedInAttackIds ?? new(),
            Notes = notes,
            MitreUrl = mitreUrl,
            Metadata = metadata
        };
    }

    #endregion

    #region Domain Behaviors

    /// <summary>
    /// Record usage of this TTP.
    /// </summary>
    public void RecordUsage(Guid? attackEventId = null)
    {
        UsageCount++;
        LastUsedAt = DateTime.UtcNow;

        if (attackEventId.HasValue && attackEventId.Value != Guid.Empty)
        {
            if (!_observedInAttackIds.Contains(attackEventId.Value))
            {
                _observedInAttackIds.Add(attackEventId.Value);
            }
        }

        // Increase confidence with more observations
        UpdateConfidenceFromUsage();
    }

    /// <summary>
    /// Update confidence score.
    /// </summary>
    public Result UpdateConfidence(int score, TTPDetectionMethod? method = null)
    {
        if (score < 0 || score > 100)
            return Result.Failure(ThreatActorErrors.InvalidConfidenceScore);

        ConfidenceScore = score;

        if (method.HasValue)
        {
            DetectionMethod = method.Value;
        }

        return Result.Success();
    }

    /// <summary>
    /// Set severity level.
    /// </summary>
    public void SetSeverity(TTPSeverity severity)
    {
        Severity = severity;
    }

    /// <summary>
    /// Mark as signature TTP for this actor.
    /// </summary>
    public void MarkAsSignature()
    {
        IsSignatureTTP = true;
    }

    /// <summary>
    /// Unmark as signature TTP.
    /// </summary>
    public void UnmarkAsSignature()
    {
        IsSignatureTTP = false;
    }

    /// <summary>
    /// Add notes about TTP usage.
    /// </summary>
    public Result AddNotes(string notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
            return Result.Failure(ThreatActorErrors.InvalidNote);

        Notes = string.IsNullOrEmpty(Notes)
            ? notes.Trim()
            : $"{Notes}\n---\n{notes.Trim()}";

        return Result.Success();
    }

    /// <summary>
    /// Set sub-technique information.
    /// </summary>
    public void SetSubTechnique(string subTechniqueId, string subTechniqueName)
    {
        SubTechniqueId = subTechniqueId?.Trim().ToUpperInvariant();
        SubTechniqueName = subTechniqueName?.Trim();
        MitreUrl = GenerateMitreUrl(TechniqueId, SubTechniqueId);
    }

    /// <summary>
    /// Set metadata.
    /// </summary>
    public void SetMetadata(string metadata)
    {
        Metadata = metadata;
    }

    #endregion

    #region Query Methods

    /// <summary>
    /// Get days since first observed.
    /// </summary>
    public int GetDaysSinceFirstUsed() => (DateTime.UtcNow - FirstUsedAt).Days;

    /// <summary>
    /// Get days since last observed.
    /// </summary>
    public int GetDaysSinceLastUsed() => (DateTime.UtcNow - LastUsedAt).Days;

    /// <summary>
    /// Check if TTP is recently used.
    /// </summary>
    public bool IsRecentlyUsed(int days = 7) => GetDaysSinceLastUsed() <= days;

    /// <summary>
    /// Check if high confidence identification.
    /// </summary>
    public bool IsHighConfidence() => ConfidenceScore >= 80;

    /// <summary>
    /// Check if TTP is frequently used.
    /// </summary>
    public bool IsFrequentlyUsed(int threshold = 5) => UsageCount >= threshold;

    /// <summary>
    /// Get full technique identifier (including sub-technique).
    /// </summary>
    public string GetFullTechniqueId() => string.IsNullOrEmpty(SubTechniqueId)
        ? TechniqueId
        : SubTechniqueId;

    /// <summary>
    /// Get full technique name (including sub-technique).
    /// </summary>
    public string GetFullTechniqueName() => string.IsNullOrEmpty(SubTechniqueName)
        ? TechniqueName
        : $"{TechniqueName}: {SubTechniqueName}";

    /// <summary>
    /// Get number of attacks where this TTP was observed.
    /// </summary>
    public int GetAttackCount() => _observedInAttackIds.Count;

    #endregion

    #region Private Methods

    private void UpdateConfidenceFromUsage()
    {
        // Increase confidence based on usage frequency
        var newConfidence = UsageCount switch
        {
            >= 20 => Math.Max(ConfidenceScore, 95),
            >= 10 => Math.Max(ConfidenceScore, 85),
            >= 5 => Math.Max(ConfidenceScore, 75),
            >= 3 => Math.Max(ConfidenceScore, 65),
            _ => ConfidenceScore
        };

        ConfidenceScore = Math.Min(100, newConfidence);
    }

    private static string? GenerateMitreUrl(string techniqueId, string? subTechniqueId)
    {
        if (string.IsNullOrEmpty(techniqueId))
            return null;

        var baseId = techniqueId.Replace(".", "/");
        return $"https://attack.mitre.org/techniques/{baseId}/";
    }

    #endregion
}

/// <summary>
/// How the TTP was detected.
/// </summary>
public enum TTPDetectionMethod
{
    Automatic = 0,      // AI/ML detection
    Manual = 1,         // Analyst identification
    RuleBased = 2,      // Signature/rule match
    Behavioral = 3,     // Behavior analysis
    External = 4        // External threat intel
}

/// <summary>
/// Severity of the TTP.
/// </summary>
public enum TTPSeverity
{
    Unknown = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}
