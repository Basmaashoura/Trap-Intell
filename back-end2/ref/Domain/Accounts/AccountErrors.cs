using SharedKernel;

namespace Domain.Accounts;

public static class AccountErrors
{
    public static Error NotFound(Guid accountId) => Error.NotFound(
        "Accounts.NotFound",
        $"The account with the Id = '{accountId}' was not found");

    public static readonly Error NameNotUnique = Error.Conflict(
        "Accounts.NameNotUnique",
        "An account with this name already exists");

    public static readonly Error HasActiveProjects = Error.Failure(
        "Accounts.HasActiveProjects",
        "Cannot deactivate account with active projects");

    public static readonly Error HasActiveContacts = Error.Failure(
        "Accounts.HasActiveContacts",
        "Cannot delete account with active contacts");

    public static readonly Error AlreadyInactive = Error.Failure(
        "Accounts.AlreadyInactive",
        "The account is already inactive");
}
