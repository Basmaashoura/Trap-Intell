using System.Linq.Expressions;

namespace Application.Common.Filtering;

/// <summary>
/// Provides search configuration for an entity.
/// Inherit from this class to define searchable fields for each entity.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public abstract class SearchConfiguration<TEntity> where TEntity : class
{
    private readonly List<Expression<Func<TEntity, string>>> _searchExpressions = [];

    /// <summary>
    /// Registers a searchable field.
    /// </summary>
    protected void AddSearchableField(Expression<Func<TEntity, string>> expression)
    {
        _searchExpressions.Add(expression);
    }

    /// <summary>
    /// Gets all search expressions.
    /// </summary>
    public IReadOnlyList<Expression<Func<TEntity, string>>> GetSearchExpressions()
    {
        return _searchExpressions;
    }
}
