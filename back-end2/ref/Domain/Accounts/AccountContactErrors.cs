using SharedKernel;

namespace Domain.Accounts;

public static class AccountContactErrors
{
    public static Error NotFound(Guid contactId) => Error.NotFound(
        "AccountContacts.NotFound",
        $"The account contact with the Id = '{contactId}' was not found");

    public static readonly Error UserAlreadyLinked = Error.Conflict(
        "AccountContacts.UserAlreadyLinked",
        "This user is already linked to the account");

    public static readonly Error AlreadyContact = Error.Conflict(
        "AccountContacts.AlreadyContact",
        "This user is already a contact of this account");

    public static readonly Error CannotRemovePrimaryContact = Error.Failure(
        "AccountContacts.CannotRemovePrimaryContact",
        "Cannot remove the primary contact. Assign another primary contact first.");

    public static readonly Error AccountMustHavePrimaryContact = Error.Validation(
        "AccountContacts.AccountMustHavePrimaryContact",
        "Account must have at least one primary contact");

    public static readonly Error UserNotEndUser = Error.Validation(
        "AccountContacts.UserNotEndUser",
        "Only EndUser accounts can be linked to accounts");

    public static readonly Error InviteAlreadyAccepted = Error.Failure(
        "AccountContacts.InviteAlreadyAccepted",
        "This invitation has already been accepted");

    public static readonly Error InviteNotFound = Error.NotFound(
        "AccountContacts.InviteNotFound",
        "Invitation not found or has been revoked");

    public static readonly Error CannotInviteContacts = Error.Failure(
        "AccountContacts.CannotInviteContacts",
        "You do not have permission to invite contacts");

    public static readonly Error ContactInactive = Error.Failure(
        "AccountContacts.ContactInactive",
        "This contact is inactive");

    public static readonly Error InsufficientPermissions = Error.Failure(
        "AccountContacts.InsufficientPermissions",
        "You do not have permission to perform this action");

    public static readonly Error ContactNotFound = Error.NotFound(
        "AccountContacts.NotFound",
        "The account contact was not found");

    public static readonly Error CannotModifyPrimaryContactPermissions = Error.Failure(
        "AccountContacts.CannotModifyPrimaryContactPermissions",
        "Cannot modify primary contact's permissions. They always have full access.");

    public static readonly Error NotAuthorizedToManageContacts = Error.Failure(
        "AccountContacts.NotAuthorizedToManageContacts",
        "You are not authorized to manage contacts for this account");
}
