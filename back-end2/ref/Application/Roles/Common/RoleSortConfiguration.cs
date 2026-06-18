using Application.Common.Sorting;
using Domain.Permissions;

namespace Application.Roles.Common;

/// <summary>
/// Sorting configuration for Role entity.
/// </summary>
public sealed class RoleSortConfiguration : SortConfiguration<Role>
{
    public static readonly RoleSortConfiguration Instance = new();

    public override string DefaultSortField => "CREATEDAT";

    private RoleSortConfiguration()
    {
        AddSortableField("Name", r => r.Name);
        AddSortableField("CreatedAt", r => r.CreatedAt);
        AddSortableField("IsActive", r => r.IsActive);
        AddSortableField("IsSystemRole", r => r.IsSystemRole);
    }
}
