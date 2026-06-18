using SharedKernel;

namespace Domain.Users;

public static class UserErrors
{
    public static Error NotFound(Guid userId) => Error.NotFound(
        "Users.NotFound",
        $"The user with the Id = '{userId}' was not found");

    public static Error NotFoundByEmail(string email) => Error.NotFound(
        "Users.NotFoundByEmail",
        $"The user with email '{email}' was not found");

    public static Error Unauthorized() => Error.Failure(
        "Users.Unauthorized",
        "You are not authorized to perform this action.");

    public static readonly Error EmailNotUnique = Error.Conflict(
        "Users.EmailNotUnique",
        "The provided email is not unique");

    public static readonly Error UserInactive = Error.Failure(
        "Users.Inactive",
        "This user account has been deactivated");

    public static readonly Error UserSuspended = Error.Failure(
        "Users.Suspended",
        "This user account has been suspended");




    public static readonly Error CannotDeactivateSuperAdmin = Error.Failure(
        "Users.CannotDeactivateSuperAdmin",
        "Cannot deactivate a Super Admin user");

    public static Error ProfileNotFound(Guid userId) => Error.NotFound(
        "Users.ProfileNotFound",
        $"Profile for user with Id = '{userId}' was not found");

    public static Error SessionNotFound(Guid sessionId) => Error.NotFound(
        "Users.SessionNotFound",
        $"Session with Id = '{sessionId}' was not found");

    public static readonly Error CannotModifyOwnRole = Error.Failure(
        "Users.CannotModifyOwnRole",
        "You cannot modify your own roles");

    public static readonly Error RoleAlreadyAssigned = Error.Conflict(
        "Users.RoleAlreadyAssigned",
        "This role is already assigned to the user");

    public static readonly Error InvalidAccountType = Error.Validation(
        "Users.InvalidAccountType",
        "Invalid account type for this operation");

    public static readonly Error OldPasswordIncorrect = Error.Validation(
        "Users.OldPasswordIncorrect",
        "The old password is incorrect");

    public static readonly Error PasswordTooWeak = Error.Validation(
        "Users.PasswordTooWeak",
        "The password does not meet the minimum security requirements");

    public static readonly Error PasswordSameAsOld = Error.Validation(
        "Users.PasswordSameAsOld",
        "The new password cannot be the same as the old password");
}

