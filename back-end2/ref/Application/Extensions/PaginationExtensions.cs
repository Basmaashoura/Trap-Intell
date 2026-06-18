using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Extensions;

/// <summary>
/// Extension methods for pagination.
/// </summary>
public static class PaginationExtensions
{
    /// <summary>
    /// Creates a paged result from an IQueryable.
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Get total count
        int totalCount = await query.CountAsync(cancellationToken);

        // If no items, return empty result
        if (totalCount == 0)
        {
            return PagedResult<T>.Empty(pageNumber, pageSize);
        }

        // Get items for current page
        List<T> items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<T>.Create(items, pageNumber, pageSize, totalCount);
    }

    /// <summary>
    /// Creates a paged result from a list (already in memory).
    /// </summary>
    public static PagedResult<T> ToPagedResult<T>(
        this IReadOnlyList<T> items,
        int pageNumber,
        int pageSize,
        int totalCount)
    {
        return PagedResult<T>.Create(items, pageNumber, pageSize, totalCount);
    }
}
