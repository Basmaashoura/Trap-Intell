using System.Linq.Expressions;

namespace Application.Common.Sorting;

/// <summary>
/// Provides sorting configuration for an entity.
/// Inherit from this class to define sortable fields for each entity.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public abstract class SortConfiguration<TEntity> where TEntity : class
{
    private readonly Dictionary<string, SortableField<TEntity>> _sortableFields = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the default sort field name.
    /// </summary>
    public abstract string DefaultSortField { get; }

    /// <summary>
    /// Gets whether default sort is descending.
    /// </summary>
    public virtual bool DefaultDescending => true;

    /// <summary>
    /// Registers a sortable field.
    /// </summary>
    protected void AddSortableField(string name, Expression<Func<TEntity, object>> expression)
    {
        var field = new SortableField<TEntity>(name, expression);
        _sortableFields[field.Name] = field;
    }

    /// <summary>
    /// Gets all registered sortable field names.
    /// </summary>
    public IReadOnlyCollection<string> GetSortableFieldNames()
    {
        return _sortableFields.Keys;
    }

    /// <summary>
    /// Checks if a field name is sortable.
    /// </summary>
    public bool IsSortable(string fieldName)
    {
        return _sortableFields.ContainsKey(fieldName.ToUpperInvariant());
    }

    /// <summary>
    /// Gets the sort expression for a field.
    /// </summary>
    public Expression<Func<TEntity, object>>? GetSortExpression(string fieldName)
    {
        if (_sortableFields.TryGetValue(fieldName.ToUpperInvariant(), out SortableField<TEntity>? field))
        {
            return field.Expression;
        }

        return null;
    }
}
