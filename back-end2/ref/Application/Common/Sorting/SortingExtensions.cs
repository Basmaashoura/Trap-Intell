using System.Linq.Expressions;

namespace Application.Common.Sorting;

/// <summary>
/// Extension methods for applying sorting to IQueryable.
/// </summary>
public static class SortingExtensions
{
    /// <summary>
    /// Applies sorting to the query using the provided configuration.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="query">The query to sort.</param>
    /// <param name="configuration">The sort configuration.</param>
    /// <param name="sortBy">The field to sort by.</param>
    /// <param name="sortDirection">The sort direction (asc/desc).</param>
    /// <returns>The sorted query.</returns>
    public static IQueryable<TEntity> ApplySort<TEntity>(
        this IQueryable<TEntity> query,
        SortConfiguration<TEntity> configuration,
        string? sortBy,
        string? sortDirection) where TEntity : class
    {
        // Use default if not specified or invalid
        string fieldName = !string.IsNullOrWhiteSpace(sortBy) && configuration.IsSortable(sortBy)
            ? sortBy
            : configuration.DefaultSortField;

        bool isDescending = string.IsNullOrWhiteSpace(sortDirection)
            ? configuration.DefaultDescending
            : sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);

        Expression<Func<TEntity, object>>? expression = configuration.GetSortExpression(fieldName);

        if (expression is null)
        {
            // Fallback to default
            expression = configuration.GetSortExpression(configuration.DefaultSortField);
            if (expression is null)
            {
                return query; // No sorting possible
            }
        }

        return isDescending
            ? query.OrderByDescending(expression)
            : query.OrderBy(expression);
    }

    /// <summary>
    /// Validates if a sort field is valid.
    /// </summary>
    public static bool IsValidSortField<TEntity>(
        this SortConfiguration<TEntity> configuration,
        string? sortBy) where TEntity : class
    {
        return string.IsNullOrWhiteSpace(sortBy) || configuration.IsSortable(sortBy);
    }
}
