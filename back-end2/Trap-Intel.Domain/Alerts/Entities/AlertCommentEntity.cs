using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Domain.Alerts.Entities;

/// <summary>
/// Represents a comment on an alert.
/// Child entity owned by Alert aggregate.
/// Supports editing, deletion, and internal/external visibility.
/// </summary>
public class AlertCommentEntity : Entity<Guid>
{
    // Private constructor for EF
    private AlertCommentEntity() { }

    private AlertCommentEntity(
        Guid id,
        Guid alertId,
        string content,
        Guid authorUserId,
        bool isInternal)
        : base(id)
    {
        AlertId = alertId;
        Content = content;
        AuthorUserId = authorUserId;
        IsInternal = isInternal;
        CreatedAt = DateTime.UtcNow;
        IsEdited = false;
        IsDeleted = false;
    }

    #region Properties

    /// <summary>
    /// Parent alert ID.
    /// </summary>
    public Guid AlertId { get; private set; }

    /// <summary>
    /// Comment content.
    /// </summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// User who authored the comment.
    /// </summary>
    public Guid AuthorUserId { get; private set; }

    /// <summary>
    /// When the comment was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the comment was last edited (if edited).
    /// </summary>
    public DateTime? EditedAt { get; private set; }

    /// <summary>
    /// User who last edited the comment (if edited).
    /// </summary>
    public Guid? EditedByUserId { get; private set; }

    /// <summary>
    /// Whether the comment has been edited.
    /// </summary>
    public bool IsEdited { get; private set; }

    /// <summary>
    /// Whether this is an internal note (not visible externally).
    /// </summary>
    public bool IsInternal { get; private set; }

    /// <summary>
    /// Whether the comment has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// When the comment was deleted (if deleted).
    /// </summary>
    public DateTime? DeletedAt { get; private set; }

    /// <summary>
    /// User who deleted the comment (if deleted).
    /// </summary>
    public Guid? DeletedByUserId { get; private set; }

    /// <summary>
    /// Parent comment ID for threaded replies.
    /// </summary>
    public Guid? ParentCommentId { get; private set; }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Create a new comment.
    /// </summary>
    public static Result<AlertCommentEntity> Create(
        Guid alertId,
        string content,
        Guid authorUserId,
        bool isInternal = false,
        Guid? parentCommentId = null)
    {
        if (alertId == Guid.Empty)
            return Result.Failure<AlertCommentEntity>(AlertErrors.InvalidAlertId);

        if (authorUserId == Guid.Empty)
            return Result.Failure<AlertCommentEntity>(AlertErrors.InvalidUserId);

        if (string.IsNullOrWhiteSpace(content))
            return Result.Failure<AlertCommentEntity>(AlertErrors.InvalidComment);

        if (content.Length > 10000)
            return Result.Failure<AlertCommentEntity>(AlertErrors.CommentTooLong);

        var comment = new AlertCommentEntity(
            Guid.NewGuid(),
            alertId,
            content.Trim(),
            authorUserId,
            isInternal)
        {
            ParentCommentId = parentCommentId
        };

        return Result.Success(comment);
    }

    /// <summary>
    /// Reconstruct from database.
    /// </summary>
    public static AlertCommentEntity Reconstruct(
        Guid id,
        Guid alertId,
        string content,
        Guid authorUserId,
        DateTime createdAt,
        DateTime? editedAt,
        Guid? editedByUserId,
        bool isEdited,
        bool isInternal,
        bool isDeleted,
        DateTime? deletedAt,
        Guid? deletedByUserId,
        Guid? parentCommentId)
    {
        return new AlertCommentEntity
        {
            Id = id,
            AlertId = alertId,
            Content = content,
            AuthorUserId = authorUserId,
            CreatedAt = createdAt,
            EditedAt = editedAt,
            EditedByUserId = editedByUserId,
            IsEdited = isEdited,
            IsInternal = isInternal,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt,
            DeletedByUserId = deletedByUserId,
            ParentCommentId = parentCommentId
        };
    }

    #endregion

    #region Domain Behaviors

    /// <summary>
    /// Edit the comment content.
    /// </summary>
    public Result Edit(string newContent, Guid editorUserId)
    {
        if (IsDeleted)
            return Result.Failure(AlertErrors.CommentDeleted);

        if (editorUserId == Guid.Empty)
            return Result.Failure(AlertErrors.InvalidUserId);

        if (string.IsNullOrWhiteSpace(newContent))
            return Result.Failure(AlertErrors.InvalidComment);

        if (newContent.Length > 10000)
            return Result.Failure(AlertErrors.CommentTooLong);

        Content = newContent.Trim();
        EditedAt = DateTime.UtcNow;
        EditedByUserId = editorUserId;
        IsEdited = true;

        return Result.Success();
    }

    /// <summary>
    /// Soft delete the comment.
    /// </summary>
    public Result Delete(Guid deleterUserId)
    {
        if (IsDeleted)
            return Result.Success(); // Already deleted

        if (deleterUserId == Guid.Empty)
            return Result.Failure(AlertErrors.InvalidUserId);

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedByUserId = deleterUserId;
        Content = "[Comment deleted]";

        return Result.Success();
    }

    /// <summary>
    /// Restore a deleted comment (if within time limit).
    /// </summary>
    public Result Restore(Guid restorerUserId, string originalContent)
    {
        if (!IsDeleted)
            return Result.Failure(AlertErrors.CommentNotDeleted);

        if (restorerUserId == Guid.Empty)
            return Result.Failure(AlertErrors.InvalidUserId);

        // Can only restore within 24 hours
        if (DeletedAt.HasValue && DateTime.UtcNow - DeletedAt.Value > TimeSpan.FromHours(24))
            return Result.Failure(AlertErrors.CommentRestoreExpired);

        IsDeleted = false;
        DeletedAt = null;
        DeletedByUserId = null;
        Content = originalContent;

        return Result.Success();
    }

    /// <summary>
    /// Change visibility (internal/external).
    /// </summary>
    public Result ChangeVisibility(bool isInternal, Guid changedByUserId)
    {
        if (IsDeleted)
            return Result.Failure(AlertErrors.CommentDeleted);

        IsInternal = isInternal;
        EditedAt = DateTime.UtcNow;
        EditedByUserId = changedByUserId;

        return Result.Success();
    }

    #endregion

    #region Query Methods

    /// <summary>
    /// Check if comment is visible to external users.
    /// </summary>
    public bool IsVisibleExternally() => !IsInternal && !IsDeleted;

    /// <summary>
    /// Check if comment can be edited by user.
    /// </summary>
    public bool CanBeEditedBy(Guid userId, bool isAdmin = false)
    {
        if (IsDeleted) return false;
        if (isAdmin) return true;
        return AuthorUserId == userId;
    }

    /// <summary>
    /// Check if comment is a reply.
    /// </summary>
    public bool IsReply() => ParentCommentId.HasValue;

    /// <summary>
    /// Get time since creation.
    /// </summary>
    public TimeSpan GetAge() => DateTime.UtcNow - CreatedAt;

    #endregion
}
