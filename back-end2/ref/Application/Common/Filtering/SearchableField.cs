using System.Linq.Expressions;

namespace Application.Common.Filtering;

/// <summary>
/// Defines a searchable field for an entity.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public sealed class SearchableField<TEntity>
{
    /// <summary>
    /// The field name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The expression to get the string value to search.
    /// </summary>
    public Expression<Func<TEntity, string>> Expression { get; }

    public SearchableField(string name, Expression<Func<TEntity, string>> expression)
    {
        Name = name;
        Expression = expression;
    }
}
