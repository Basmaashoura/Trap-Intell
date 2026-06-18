using Application.Common.Filtering;
using Domain.Accounts;

namespace Application.Accounts.Common;

/// <summary>
/// Search configuration for Account entity.
/// </summary>
public sealed class AccountSearchConfiguration : SearchConfiguration<Account>
{
    public static readonly AccountSearchConfiguration Instance = new();

    private AccountSearchConfiguration()
    {
        AddSearchableField(a => a.Name);
        AddSearchableField(a => a.Industry ?? string.Empty);
        AddSearchableField(a => a.Website ?? string.Empty);
        AddSearchableField(a => a.Address ?? string.Empty);
    }
}
