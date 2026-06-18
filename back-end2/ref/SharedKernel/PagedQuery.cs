using System.ComponentModel;

namespace SharedKernel;

/// <summary>
/// Base class for paginated queries.
/// </summary>
public abstract record PagedQuery
{
    private const int MaxPageSize = 100;
    private int _pageSize = 10;
    private int _pageNumber = 1;

    /// <summary>
    /// Page number (1-based).
    /// </summary>
    [DefaultValue(1)]
    public int PageNumber
    {
        get => _pageNumber;
        init => _pageNumber = value < 1 ? 1 : value;
    }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    [DefaultValue(10)]
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value < 1 ? 10 : Math.Min(value, MaxPageSize);
    }

    /// <summary>
    /// Number of items to skip.
    /// </summary>
    public int Skip => (PageNumber - 1) * PageSize;

    /// <summary>
    /// Number of items to take.
    /// </summary>
    public int Take => PageSize;
}
