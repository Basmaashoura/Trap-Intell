using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Invitations;

public static class InvitationErrors
{
    #region Validation Errors

    public static readonly Error InvalidOrganizationId = Error.Custom(
        "Invitation.InvalidOrganizationId",
        "Organization ID cannot be empty");

    public static readonly Error InvalidUserId = Error.Custom(
        "Invitation.InvalidUserId",
        "User ID cannot be empty");

    public static readonly Error InvalidEmail = Error.Custom(
        "Invitation.InvalidEmail",
        "Email address cannot be empty");

    public static readonly Error InvalidEmailFormat = Error.Custom(
        "Invitation.InvalidEmailFormat",
        "Email address format is invalid");

    public static readonly Error InvalidRevocationReason = Error.Custom(
        "Invitation.InvalidRevocationReason",
        "Revocation reason cannot be empty");

    public static readonly Error InvalidExpirationDays = Error.Custom(
        "Invitation.InvalidExpirationDays",
        "Expiration days must be between 1 and 30");

    #endregion

    #region Status Errors

    public static readonly Error InvitationNotPending = Error.Custom(
        "Invitation.NotPending",
        "Invitation is not in pending status");

    public static readonly Error InvitationExpired = Error.Custom(
        "Invitation.Expired",
        "Invitation has expired");

    public static readonly Error InvitationNotExpiredYet = Error.Custom(
        "Invitation.NotExpiredYet",
        "Invitation has not expired yet");

    public static readonly Error AlreadyRevoked = Error.Custom(
        "Invitation.AlreadyRevoked",
        "Invitation is already revoked");

    public static readonly Error CannotRevokeAcceptedInvitation = Error.Custom(
        "Invitation.CannotRevokeAccepted",
        "Cannot revoke an accepted invitation");

    public static readonly Error CannotExtendNonPendingInvitation = Error.Custom(
        "Invitation.CannotExtendNonPending",
        "Can only extend pending or expired invitations");

    public static readonly Error CannotUpdateNonPendingInvitation = Error.Custom(
        "Invitation.CannotUpdateNonPending",
        "Can only update pending invitations");

    public static readonly Error CannotResendAcceptedInvitation = Error.Custom(
        "Invitation.CannotResendAccepted",
        "Cannot resend an accepted invitation");

    public static readonly Error CannotResendRevokedInvitation = Error.Custom(
        "Invitation.CannotResendRevoked",
        "Cannot resend a revoked invitation");

    #endregion

    #region Business Rule Errors

    public static readonly Error MaxRemindersReached = Error.Custom(
        "Invitation.MaxRemindersReached",
        "Maximum number of reminders (3) has been reached");

    public static readonly Error UserAlreadyMember = Error.Custom(
        "Invitation.UserAlreadyMember",
        "User is already a member of this organization");

    public static readonly Error PendingInvitationExists = Error.Custom(
        "Invitation.PendingExists",
        "A pending invitation already exists for this email");

    public static readonly Error MaxPendingInvitationsReached = Error.Custom(
        "Invitation.MaxPendingReached",
        "Maximum number of pending invitations reached");

    public static readonly Error InvalidToken = Error.Custom(
        "Invitation.InvalidToken",
        "Invitation token is invalid");

    #endregion

    #region Query Errors

    public static readonly Error InvitationNotFound = Error.Custom(
        "Invitation.NotFound",
        "Invitation not found");

    #endregion

    #region Factory Methods

    public static Error NotFoundById(Guid invitationId) => Error.Custom(
        "Invitation.NotFound",
        $"Invitation with ID '{invitationId}' not found");

    public static Error NotFoundByToken(string token) => Error.Custom(
        "Invitation.NotFound",
        "Invitation with the provided token not found");

    public static Error AlreadyInvited(string email) => Error.Custom(
        "Invitation.AlreadyInvited",
        $"An invitation has already been sent to '{email}'");

    public static Error AlreadyMember(string email) => Error.Custom(
        "Invitation.AlreadyMember",
        $"'{email}' is already a member of this organization");

    #endregion
}
