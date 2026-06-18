using Application.Common.Sorting;
using Domain.Accounts;

namespace Application.Accounts.Common;

/// <summary>
/// Sorting configuration for Account entity.
/// </summary>
public sealed class AccountSortConfiguration : SortConfiguration<Account>
{
    public static readonly AccountSortConfiguration Instance = new();

    public override string DefaultSortField => "CREATEDAT";

    private AccountSortConfiguration()
    {
        AddSortableField("Name", a => a.Name);
        AddSortableField("Industry", a => a.Industry ?? string.Empty);
        AddSortableField("CreatedAt", a => a.CreatedAt);
        AddSortableField("IsActive", a => a.IsActive);
    }
}
