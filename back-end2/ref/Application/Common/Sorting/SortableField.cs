using System.Linq.Expressions;

namespace Application.Common.Sorting;

/// <summary>
/// Defines a sortable field for an entity.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public sealed class SortableField<TEntity>
{
    /// <summary>
    /// The field name (case-insensitive).
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The expression to sort by.
    /// </summary>
    public Expression<Func<TEntity, object>> Expression { get; }

    public SortableField(string name, Expression<Func<TEntity, object>> expression)
    {
        Name = name.ToUpperInvariant();
        Expression = expression;
    }
}
