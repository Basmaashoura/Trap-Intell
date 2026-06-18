using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Domain.ThreatActors.Entities;

/// <summary>
/// Represents a behavior pattern observed from a threat actor.
/// Child entity owned by ThreatActor aggregate.
/// Tracks pattern occurrences and confidence.
/// </summary>
public class BehaviorPatternEntity : Entity<Guid>
{
    private List<Guid> _observedInAttackIds = new();

    // Private constructor for EF
    private BehaviorPatternEntity() { }

    private BehaviorPatternEntity(
        Guid id,
        Guid threatActorId,
        string category,
        string description)
        : base(id)
    {
        ThreatActorId = threatActorId;
        Category = category;
        Description = description;
        Occurrences = 1;
        FirstObservedAt = DateTime.UtcNow;
        LastObservedAt = DateTime.UtcNow;
        ConfidenceScore = 50;
        Severity = PatternSeverity.Medium;
        DetectedByAI = false;
    }

    #region Properties

    /// <summary>
    /// Parent threat actor ID.
    /// </summary>
    public Guid ThreatActorId { get; private set; }

    /// <summary>
    /// Pattern category (e.g., "Timing", "TargetSelection", "Technique").
    /// </summary>
    public string Category { get; private set; } = string.Empty;

    /// <summary>
    /// Detailed pattern description.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Pattern type classification.
    /// </summary>
    public BehaviorPatternType PatternType { get; private set; } = BehaviorPatternType.Unknown;

    /// <summary>
    /// How serious this pattern is.
    /// </summary>
    public PatternSeverity Severity { get; private set; }

    /// <summary>
    /// Number of times this pattern was observed.
    /// </summary>
    public int Occurrences { get; private set; }

    /// <summary>
    /// When this pattern was first observed.
    /// </summary>
    public DateTime FirstObservedAt { get; private set; }

    /// <summary>
    /// When this pattern was most recently observed.
    /// </summary>
    public DateTime LastObservedAt { get; private set; }

    /// <summary>
    /// Confidence in pattern identification (0-100).
    /// </summary>
    public int ConfidenceScore { get; private set; }

    /// <summary>
    /// Whether this pattern was detected by AI.
    /// </summary>
    public bool DetectedByAI { get; private set; }

    /// <summary>
    /// Whether this is a distinctive pattern for this actor.
    /// </summary>
    public bool IsDistinctive { get; private set; }

    /// <summary>
    /// Attack event IDs where this pattern was observed.
    /// </summary>
    public IReadOnlyList<Guid> ObservedInAttackIds => _observedInAttackIds.AsReadOnly();

    /// <summary>
    /// User who identified this pattern (if manual).
    /// </summary>
    public Guid? IdentifiedByUserId { get; private set; }

    /// <summary>
    /// Additional notes about the pattern.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Pattern indicators/evidence as JSON.
    /// </summary>
    public string? Indicators { get; private set; }

    /// <summary>
    /// Additional metadata as JSON.
    /// </summary>
    public string? Metadata { get; private set; }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Create a new behavior pattern entity.
    /// </summary>
    public static Result<BehaviorPatternEntity> Create(
        Guid threatActorId,
        string category,
        string description,
        BehaviorPatternType? patternType = null,
        bool detectedByAI = false,
        Guid? identifiedByUserId = null)
    {
        if (threatActorId == Guid.Empty)
            return Result.Failure<BehaviorPatternEntity>(ThreatActorErrors.InvalidThreatActorId);

        if (string.IsNullOrWhiteSpace(category))
            return Result.Failure<BehaviorPatternEntity>(ThreatActorErrors.InvalidPatternCategory);

        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure<BehaviorPatternEntity>(ThreatActorErrors.InvalidPatternDescription);

        var entity = new BehaviorPatternEntity(
            Guid.NewGuid(),
            threatActorId,
            category.Trim(),
            description.Trim())
        {
            PatternType = patternType ?? BehaviorPatternType.Unknown,
            DetectedByAI = detectedByAI,
            IdentifiedByUserId = identifiedByUserId
        };

        return Result.Success(entity);
    }

    /// <summary>
    /// Reconstruct from database.
    /// </summary>
    public static BehaviorPatternEntity Reconstruct(
        Guid id,
        Guid threatActorId,
        string category,
        string description,
        BehaviorPatternType patternType,
        PatternSeverity severity,
        int occurrences,
        DateTime firstObservedAt,
        DateTime lastObservedAt,
        int confidenceScore,
        bool detectedByAI,
        bool isDistinctive,
        List<Guid>? observedInAttackIds,
        Guid? identifiedByUserId,
        string? notes,
        string? indicators,
        string? metadata)
    {
        return new BehaviorPatternEntity
        {
            Id = id,
            ThreatActorId = threatActorId,
            Category = category,
            Description = description,
            PatternType = patternType,
            Severity = severity,
            Occurrences = occurrences,
            FirstObservedAt = firstObservedAt,
            LastObservedAt = lastObservedAt,
            ConfidenceScore = confidenceScore,
            DetectedByAI = detectedByAI,
            IsDistinctive = isDistinctive,
            _observedInAttackIds = observedInAttackIds ?? new(),
            IdentifiedByUserId = identifiedByUserId,
            Notes = notes,
            Indicators = indicators,
            Metadata = metadata
        };
    }

    #endregion

    #region Domain Behaviors

    /// <summary>
    /// Record occurrence of this pattern.
    /// </summary>
    public void RecordOccurrence(Guid? attackEventId = null)
    {
        Occurrences++;
        LastObservedAt = DateTime.UtcNow;

        if (attackEventId.HasValue && attackEventId.Value != Guid.Empty)
        {
            if (!_observedInAttackIds.Contains(attackEventId.Value))
            {
                _observedInAttackIds.Add(attackEventId.Value);
            }
        }

        UpdateConfidenceFromOccurrences();
    }

    /// <summary>
    /// Update pattern description.
    /// </summary>
    public Result UpdateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure(ThreatActorErrors.InvalidPatternDescription);

        Description = description.Trim();
        return Result.Success();
    }

    /// <summary>
    /// Update confidence score.
    /// </summary>
    public Result UpdateConfidence(int score)
    {
        if (score < 0 || score > 100)
            return Result.Failure(ThreatActorErrors.InvalidConfidenceScore);

        ConfidenceScore = score;
        return Result.Success();
    }

    /// <summary>
    /// Update severity level.
    /// </summary>
    public void UpdateSeverity(PatternSeverity severity)
    {
        Severity = severity;
    }

    /// <summary>
    /// Set pattern type.
    /// </summary>
    public void SetPatternType(BehaviorPatternType type)
    {
        PatternType = type;
    }

    /// <summary>
    /// Mark as distinctive pattern for this actor.
    /// </summary>
    public void MarkAsDistinctive()
    {
        IsDistinctive = true;
    }

    /// <summary>
    /// Unmark as distinctive pattern.
    /// </summary>
    public void UnmarkAsDistinctive()
    {
        IsDistinctive = false;
    }

    /// <summary>
    /// Add notes about the pattern.
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
    /// Set pattern indicators.
    /// </summary>
    public void SetIndicators(string indicators)
    {
        Indicators = indicators;
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
    public int GetDaysSinceFirstObserved() => (DateTime.UtcNow - FirstObservedAt).Days;

    /// <summary>
    /// Get days since last observed.
    /// </summary>
    public int GetDaysSinceLastObserved() => (DateTime.UtcNow - LastObservedAt).Days;

    /// <summary>
    /// Check if pattern is recently observed.
    /// </summary>
    public bool IsRecentlyObserved(int days = 7) => GetDaysSinceLastObserved() <= days;

    /// <summary>
    /// Check if high confidence pattern.
    /// </summary>
    public bool IsHighConfidence() => ConfidenceScore >= 80;

    /// <summary>
    /// Check if pattern is frequently observed.
    /// </summary>
    public bool IsFrequentlyObserved(int threshold = 5) => Occurrences >= threshold;

    /// <summary>
    /// Check if pattern is high severity.
    /// </summary>
    public bool IsHighSeverity() => Severity >= PatternSeverity.High;

    /// <summary>
    /// Get number of attacks where this pattern was observed.
    /// </summary>
    public int GetAttackCount() => _observedInAttackIds.Count;

    #endregion

    #region Private Methods

    private void UpdateConfidenceFromOccurrences()
    {
        var newConfidence = Occurrences switch
        {
            >= 20 => Math.Max(ConfidenceScore, 95),
            >= 10 => Math.Max(ConfidenceScore, 85),
            >= 5 => Math.Max(ConfidenceScore, 75),
            >= 3 => Math.Max(ConfidenceScore, 65),
            _ => ConfidenceScore
        };

        ConfidenceScore = Math.Min(100, newConfidence);
    }

    #endregion
}

/// <summary>
/// Type of behavior pattern.
/// </summary>
public enum BehaviorPatternType
{
    Unknown = 0,
    TimingPattern = 1,          // Attack timing patterns
    TargetSelection = 2,        // How targets are chosen
    TechniqueSequence = 3,      // Order of techniques used
    ToolUsage = 4,              // Specific tools used
    CommunicationPattern = 5,   // C2 communication patterns
    ExfiltrationMethod = 6,     // How data is exfiltrated
    PersistenceMethod = 7,      // How persistence is maintained
    EvasionTechnique = 8,       // How detection is avoided
    SocialEngineering = 9,      // Social engineering patterns
    GeographicTarget = 10,      // Geographic targeting
    IndustryTarget = 11,        // Industry targeting
    AttackCadence = 12,         // Frequency/rhythm of attacks
    PayloadCharacteristic = 13  // Characteristics of payloads
}

/// <summary>
/// Severity of the behavior pattern.
/// </summary>
public enum PatternSeverity
{
    Unknown = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}
