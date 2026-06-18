using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Alerts;

public static class AlertErrors
{
    #region Core Alert Errors

    public static readonly Error NotFound = Error.Custom(
        "Alert.NotFound",
        "Alert not found");

    public static readonly Error InvalidAlertId = Error.Custom(
        "Alert.InvalidAlertId",
        "Alert ID cannot be empty");

    public static readonly Error InvalidOrganizationId = Error.Custom(
        "Alert.InvalidOrganizationId",
        "Organization ID cannot be empty");

    public static readonly Error InvalidTitle = Error.Custom(
        "Alert.InvalidTitle",
        "Alert title cannot be empty");

    public static readonly Error InvalidDescription = Error.Custom(
        "Alert.InvalidDescription",
        "Alert description cannot be empty");

    public static readonly Error InvalidUserId = Error.Custom(
        "Alert.InvalidUserId",
        "User ID cannot be empty");

    public static readonly Error InvalidSource = Error.Custom(
        "Alert.InvalidSource",
        "Alert source cannot be null");

    #endregion

    #region Status Errors

    public static readonly Error AlreadyAcknowledged = Error.Custom(
        "Alert.AlreadyAcknowledged",
        "Alert has already been acknowledged");

    public static readonly Error AlreadyResolved = Error.Custom(
        "Alert.AlreadyResolved",
        "Alert has already been resolved");

    public static readonly Error AlreadyEscalated = Error.Custom(
        "Alert.AlreadyEscalated",
        "Alert is already at this escalation level");

    public static readonly Error CannotEscalateHigher = Error.Custom(
        "Alert.CannotEscalateHigher",
        "Alert is already at maximum escalation level");

    public static readonly Error CannotModifyResolved = Error.Custom(
        "Alert.CannotModifyResolved",
        "Cannot modify a resolved alert");

    public static readonly Error InvalidSnoozeDuration = Error.Custom(
        "Alert.InvalidSnoozeDuration",
        "Snooze duration must be positive");

    public static readonly Error AlreadySnoozed = Error.Custom(
        "Alert.AlreadySnoozed",
        "Alert is already snoozed");

    public static readonly Error NotSnoozed = Error.Custom(
        "Alert.NotSnoozed",
        "Alert is not snoozed");

    #endregion

    #region Validation Errors

    public static readonly Error InvalidResolution = Error.Custom(
        "Alert.InvalidResolution",
        "Resolution description cannot be empty");

    public static readonly Error InvalidReason = Error.Custom(
        "Alert.InvalidReason",
        "Reason cannot be empty");

    public static readonly Error InvalidAssignee = Error.Custom(
        "Alert.InvalidAssignee",
        "Invalid assignee user ID");

    public static readonly Error SelfAssignment = Error.Custom(
        "Alert.SelfAssignment",
        "User is already assigned to this alert");

    public static readonly Error InvalidSeverity = Error.Custom(
        "Alert.InvalidSeverity",
        "Invalid severity level");

    #endregion

    #region Comment Errors

    public static readonly Error InvalidComment = Error.Custom(
        "Alert.InvalidComment",
        "Comment content cannot be empty");

    public static readonly Error CommentTooLong = Error.Custom(
        "Alert.CommentTooLong",
        "Comment cannot exceed 10,000 characters");

    public static readonly Error CommentNotFound = Error.Custom(
        "Alert.CommentNotFound",
        "Comment not found");

    public static readonly Error CommentDeleted = Error.Custom(
        "Alert.CommentDeleted",
        "Cannot modify a deleted comment");

    public static readonly Error CommentNotDeleted = Error.Custom(
        "Alert.CommentNotDeleted",
        "Comment is not deleted");

    public static readonly Error CommentRestoreExpired = Error.Custom(
        "Alert.CommentRestoreExpired",
        "Cannot restore comment after 24 hours");

    public static readonly Error CannotEditComment = Error.Custom(
        "Alert.CannotEditComment",
        "You do not have permission to edit this comment");

    public static readonly Error CannotDeleteComment = Error.Custom(
        "Alert.CannotDeleteComment",
        "You do not have permission to delete this comment");

    #endregion

    #region Notification Errors

    public static readonly Error NoNotificationRecipients = Error.Custom(
        "Alert.NoNotificationRecipients",
        "At least one recipient is required for notification");

    public static readonly Error InvalidFailureReason = Error.Custom(
        "Alert.InvalidFailureReason",
        "Failure reason cannot be empty");

    public static readonly Error MaxRetriesExceeded = Error.Custom(
        "Alert.MaxRetriesExceeded",
        "Maximum notification retries exceeded");

    public static readonly Error CannotCancelSentNotification = Error.Custom(
        "Alert.CannotCancelSentNotification",
        "Cannot cancel a notification that has already been sent");

    public static readonly Error NotificationNotFound = Error.Custom(
        "Alert.NotificationNotFound",
        "Notification not found");

    public static readonly Error NotificationAlreadySent = Error.Custom(
        "Alert.NotificationAlreadySent",
        "Notification has already been sent");

    #endregion

    #region Escalation Errors

    public static readonly Error InvalidEscalationLevel = Error.Custom(
        "Alert.InvalidEscalationLevel",
        "Cannot escalate to same or lower level");

    public static readonly Error InvalidSLAName = Error.Custom(
        "Alert.InvalidSLAName",
        "SLA name cannot be empty");

    public static readonly Error EscalationNotFound = Error.Custom(
        "Alert.EscalationNotFound",
        "Escalation record not found");

    public static readonly Error CannotDeescalate = Error.Custom(
        "Alert.CannotDeescalate",
        "Cannot de-escalate an alert");

    #endregion

    #region Action Errors

    public static readonly Error ActionNotFound = Error.Custom(
        "Alert.ActionNotFound",
        "Action not found");

    public static readonly Error InvalidActionType = Error.Custom(
        "Alert.InvalidActionType",
        "Invalid action type");

    #endregion

    #region Factory Methods

    public static Error NotFoundById(Guid alertId) => Error.Custom(
        "Alert.NotFound",
        $"Alert with ID '{alertId}' not found");

    public static Error CommentNotFoundById(Guid commentId) => Error.Custom(
        "Alert.CommentNotFound",
        $"Comment with ID '{commentId}' not found");

    public static Error NotificationFailedDetail(string channel, string reason) => Error.Custom(
        "Alert.NotificationFailed",
        $"Notification via {channel} failed: {reason}");

    #endregion
}

