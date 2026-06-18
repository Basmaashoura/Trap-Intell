namespace Trap_Intel.Application.Abstractions.Querying;

public sealed record GlobalQueryOptions(
    int PageNumber = 1,
    int PageSize = 50,
    string? Search = null,
    string? SortBy = null,
    string SortDirection = "desc")
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;

    public int GetPageNumber()
    {
        return PageNumber < 1 ? 1 : PageNumber;
    }

    public int GetPageSize()
    {
        if (PageSize < 1)
        {
            return DefaultPageSize;
        }

        return Math.Min(PageSize, MaxPageSize);
    }

    public string? GetSearchTerm()
    {
        return string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
    }

    public bool IsSortDescending()
    {
        if (string.IsNullOrWhiteSpace(SortDirection))
        {
            return true;
        }

        return !SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase);
    }
}
