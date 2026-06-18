using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Domain.ThreatActors.Entities;

/// <summary>
/// Represents an intelligence note about a threat actor.
/// Child entity owned by ThreatActor aggregate.
/// Supports editing, deletion, and classification.
/// </summary>
public class ThreatIntelNoteEntity : Entity<Guid>
{
    // Private constructor for EF
    private ThreatIntelNoteEntity() { }

    private ThreatIntelNoteEntity(
        Guid id,
        Guid threatActorId,
        string content,
        string source,
        Guid authorUserId)
        : base(id)
    {
        ThreatActorId = threatActorId;
        Content = content;
        Source = source;
        AuthorUserId = authorUserId;
        NoteType = IntelNoteType.General;
        CreatedAt = DateTime.UtcNow;
        IsEdited = false;
        IsDeleted = false;
        IsInternal = true;
        IsPinned = false;
    }

    #region Properties

    /// <summary>
    /// Parent threat actor ID.
    /// </summary>
    public Guid ThreatActorId { get; private set; }

    /// <summary>
    /// Note content.
    /// </summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// Source of the intelligence (e.g., "Internal Analysis", "OSINT", "Partner Feed").
    /// </summary>
    public string Source { get; private set; } = string.Empty;

    /// <summary>
    /// Type/classification of the note.
    /// </summary>
    public IntelNoteType NoteType { get; private set; }

    /// <summary>
    /// User who created the note.
    /// </summary>
    public Guid AuthorUserId { get; private set; }

    /// <summary>
    /// When the note was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the note was last edited.
    /// </summary>
    public DateTime? EditedAt { get; private set; }

    /// <summary>
    /// User who last edited the note.
    /// </summary>
    public Guid? EditedByUserId { get; private set; }

    /// <summary>
    /// Whether the note has been edited.
    /// </summary>
    public bool IsEdited { get; private set; }

    /// <summary>
    /// Whether the note is soft-deleted.
    /// </summary>
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// When the note was deleted.
    /// </summary>
    public DateTime? DeletedAt { get; private set; }

    /// <summary>
    /// User who deleted the note.
    /// </summary>
    public Guid? DeletedByUserId { get; private set; }

    /// <summary>
    /// Whether this is an internal note (not shareable externally).
    /// </summary>
    public bool IsInternal { get; private set; }

    /// <summary>
    /// Whether this note is pinned/highlighted.
    /// </summary>
    public bool IsPinned { get; private set; }

    /// <summary>
    /// Confidence level in the intelligence.
    /// </summary>
    public IntelConfidenceLevel ConfidenceLevel { get; private set; } = IntelConfidenceLevel.Medium;

    /// <summary>
    /// Related attack event IDs.
    /// </summary>
    public List<Guid> RelatedAttackIds { get; private set; } = new();

    /// <summary>
    /// Tags for categorization.
    /// </summary>
    public List<string> Tags { get; private set; } = new();

    /// <summary>
    /// External reference URL.
    /// </summary>
    public string? ExternalUrl { get; private set; }

    /// <summary>
    /// Additional metadata as JSON.
    /// </summary>
    public string? Metadata { get; private set; }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Create a new intel note.
    /// </summary>
    public static Result<ThreatIntelNoteEntity> Create(
        Guid threatActorId,
        string content,
        string source,
        Guid authorUserId,
        IntelNoteType? noteType = null,
        bool isInternal = true)
    {
        if (threatActorId == Guid.Empty)
            return Result.Failure<ThreatIntelNoteEntity>(ThreatActorErrors.InvalidThreatActorId);

        if (string.IsNullOrWhiteSpace(content))
            return Result.Failure<ThreatIntelNoteEntity>(ThreatActorErrors.InvalidNote);

        if (content.Length > 10000)
            return Result.Failure<ThreatIntelNoteEntity>(ThreatActorErrors.NoteTooLong);

        if (authorUserId == Guid.Empty)
            return Result.Failure<ThreatIntelNoteEntity>(ThreatActorErrors.InvalidUserId);

        var entity = new ThreatIntelNoteEntity(
            Guid.NewGuid(),
            threatActorId,
            content.Trim(),
            source?.Trim() ?? "Internal",
            authorUserId)
        {
            NoteType = noteType ?? IntelNoteType.General,
            IsInternal = isInternal
        };

        return Result.Success(entity);
    }

    /// <summary>
    /// Reconstruct from database.
    /// </summary>
    public static ThreatIntelNoteEntity Reconstruct(
        Guid id,
        Guid threatActorId,
        string content,
        string source,
        IntelNoteType noteType,
        Guid authorUserId,
        DateTime createdAt,
        DateTime? editedAt,
        Guid? editedByUserId,
        bool isEdited,
        bool isDeleted,
        DateTime? deletedAt,
        Guid? deletedByUserId,
        bool isInternal,
        bool isPinned,
        IntelConfidenceLevel confidenceLevel,
        List<Guid>? relatedAttackIds,
        List<string>? tags,
        string? externalUrl,
        string? metadata)
    {
        return new ThreatIntelNoteEntity
        {
            Id = id,
            ThreatActorId = threatActorId,
            Content = content,
            Source = source,
            NoteType = noteType,
            AuthorUserId = authorUserId,
            CreatedAt = createdAt,
            EditedAt = editedAt,
            EditedByUserId = editedByUserId,
            IsEdited = isEdited,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt,
            DeletedByUserId = deletedByUserId,
            IsInternal = isInternal,
            IsPinned = isPinned,
            ConfidenceLevel = confidenceLevel,
            RelatedAttackIds = relatedAttackIds ?? new(),
            Tags = tags ?? new(),
            ExternalUrl = externalUrl,
            Metadata = metadata
        };
    }

    #endregion

    #region Domain Behaviors

    /// <summary>
    /// Edit the note content.
    /// </summary>
    public Result Edit(string newContent, Guid editorUserId)
    {
        if (IsDeleted)
            return Result.Failure(ThreatActorErrors.NoteDeleted);

        if (editorUserId == Guid.Empty)
            return Result.Failure(ThreatActorErrors.InvalidUserId);

        if (string.IsNullOrWhiteSpace(newContent))
            return Result.Failure(ThreatActorErrors.InvalidNote);

        if (newContent.Length > 10000)
            return Result.Failure(ThreatActorErrors.NoteTooLong);

        Content = newContent.Trim();
        EditedAt = DateTime.UtcNow;
        EditedByUserId = editorUserId;
        IsEdited = true;

        return Result.Success();
    }

    /// <summary>
    /// Soft delete the note.
    /// </summary>
    public Result Delete(Guid deleterUserId)
    {
        if (IsDeleted)
            return Result.Success();

        if (deleterUserId == Guid.Empty)
            return Result.Failure(ThreatActorErrors.InvalidUserId);

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedByUserId = deleterUserId;

        return Result.Success();
    }

    /// <summary>
    /// Restore a deleted note.
    /// </summary>
    public Result Restore(Guid restorerUserId)
    {
        if (!IsDeleted)
            return Result.Failure(ThreatActorErrors.NoteNotDeleted);

        if (restorerUserId == Guid.Empty)
            return Result.Failure(ThreatActorErrors.InvalidUserId);

        // Can only restore within 30 days
        if (DeletedAt.HasValue && DateTime.UtcNow - DeletedAt.Value > TimeSpan.FromDays(30))
            return Result.Failure(ThreatActorErrors.NoteRestoreExpired);

        IsDeleted = false;
        DeletedAt = null;
        DeletedByUserId = null;

        return Result.Success();
    }

    /// <summary>
    /// Update source information.
    /// </summary>
    public Result UpdateSource(string source)
    {
        if (IsDeleted)
            return Result.Failure(ThreatActorErrors.NoteDeleted);

        if (string.IsNullOrWhiteSpace(source))
            return Result.Failure(ThreatActorErrors.InvalidNoteSource);

        Source = source.Trim();
        return Result.Success();
    }

    /// <summary>
    /// Change note type.
    /// </summary>
    public void ChangeNoteType(IntelNoteType noteType)
    {
        NoteType = noteType;
    }

    /// <summary>
    /// Change visibility.
    /// </summary>
    public void ChangeVisibility(bool isInternal)
    {
        IsInternal = isInternal;
    }

    /// <summary>
    /// Pin the note.
    /// </summary>
    public void Pin()
    {
        IsPinned = true;
    }

    /// <summary>
    /// Unpin the note.
    /// </summary>
    public void Unpin()
    {
        IsPinned = false;
    }

    /// <summary>
    /// Update confidence level.
    /// </summary>
    public void UpdateConfidenceLevel(IntelConfidenceLevel level)
    {
        ConfidenceLevel = level;
    }

    /// <summary>
    /// Add related attack.
    /// </summary>
    public void AddRelatedAttack(Guid attackId)
    {
        if (attackId != Guid.Empty && !RelatedAttackIds.Contains(attackId))
        {
            RelatedAttackIds.Add(attackId);
        }
    }

    /// <summary>
    /// Remove related attack.
    /// </summary>
    public void RemoveRelatedAttack(Guid attackId)
    {
        RelatedAttackIds.Remove(attackId);
    }

    /// <summary>
    /// Add tag.
    /// </summary>
    public void AddTag(string tag)
    {
        var normalizedTag = tag?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(normalizedTag) && !Tags.Contains(normalizedTag))
        {
            Tags.Add(normalizedTag);
        }
    }

    /// <summary>
    /// Remove tag.
    /// </summary>
    public void RemoveTag(string tag)
    {
        var normalizedTag = tag?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(normalizedTag))
        {
            Tags.Remove(normalizedTag);
        }
    }

    /// <summary>
    /// Set external URL.
    /// </summary>
    public void SetExternalUrl(string? url)
    {
        ExternalUrl = url?.Trim();
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
    /// Check if note is visible externally.
    /// </summary>
    public bool IsVisibleExternally() => !IsInternal && !IsDeleted;

    /// <summary>
    /// Check if note can be edited by user.
    /// </summary>
    public bool CanBeEditedBy(Guid userId, bool isAdmin = false)
    {
        if (IsDeleted) return false;
        if (isAdmin) return true;
        return AuthorUserId == userId;
    }

    /// <summary>
    /// Get time since creation.
    /// </summary>
    public TimeSpan GetAge() => DateTime.UtcNow - CreatedAt;

    /// <summary>
    /// Check if note is recent.
    /// </summary>
    public bool IsRecent(int days = 7) => GetAge().TotalDays <= days;

    /// <summary>
    /// Check if note has high confidence.
    /// </summary>
    public bool IsHighConfidence() => ConfidenceLevel >= IntelConfidenceLevel.High;

    /// <summary>
    /// Check if note has tags.
    /// </summary>
    public bool HasTag(string tag)
    {
        var normalizedTag = tag?.Trim().ToLowerInvariant();
        return !string.IsNullOrEmpty(normalizedTag) && Tags.Contains(normalizedTag);
    }

    /// <summary>
    /// Get related attack count.
    /// </summary>
    public int GetRelatedAttackCount() => RelatedAttackIds.Count;

    #endregion
}

/// <summary>
/// Type of intelligence note.
/// </summary>
public enum IntelNoteType
{
    General = 0,
    ThreatAssessment = 1,
    Attribution = 2,
    TTPAnalysis = 3,
    InfrastructureAnalysis = 4,
    HistoricalContext = 5,
    Recommendation = 6,
    MitigationAdvice = 7,
    IOCReport = 8,
    CampaignSummary = 9,
    ExternalIntel = 10,
    PartnerFeed = 11,
    MediaCoverage = 12
}

/// <summary>
/// Confidence level in the intelligence.
/// </summary>
public enum IntelConfidenceLevel
{
    Unknown = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Confirmed = 4
}
