using Application.Common.Filtering;
using Domain.Permissions;

namespace Application.Roles.Common;

/// <summary>
/// Search configuration for Role entity.
/// </summary>
public sealed class RoleSearchConfiguration : SearchConfiguration<Role>
{
    public static readonly RoleSearchConfiguration Instance = new();

    private RoleSearchConfiguration()
    {
        AddSearchableField(r => r.Name);
        AddSearchableField(r => r.Description ?? string.Empty);
    }
}
