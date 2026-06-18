using Application.Common.Filtering;
using Domain.Users;

namespace Application.Users.Common;

/// <summary>
/// Search configuration for User entity.
/// </summary>
public sealed class UserSearchConfiguration : SearchConfiguration<User>
{
    public static readonly UserSearchConfiguration Instance = new();

    private UserSearchConfiguration()
    {
        AddSearchableField(u => u.Email);
        AddSearchableField(u => u.FirstName);
        AddSearchableField(u => u.LastName);
    }
}
