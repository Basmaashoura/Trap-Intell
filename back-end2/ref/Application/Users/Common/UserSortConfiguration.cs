using Application.Common.Sorting;
using Domain.Users;

namespace Application.Users.Common;

/// <summary>
/// Sorting configuration for User entity.
/// </summary>
public sealed class UserSortConfiguration : SortConfiguration<User>
{
    public static readonly UserSortConfiguration Instance = new();

    public override string DefaultSortField => "CREATEDAT";

    private UserSortConfiguration()
    {
        AddSortableField("Email", u => u.Email);
        AddSortableField("FirstName", u => u.FirstName);
        AddSortableField("LastName", u => u.LastName);
        AddSortableField("CreatedAt", u => u.CreatedAt);
        AddSortableField("Status", u => u.Status);
        AddSortableField("AccountType", u => u.AccountType);
    }
}
