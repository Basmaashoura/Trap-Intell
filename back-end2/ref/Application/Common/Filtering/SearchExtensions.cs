using System.Linq.Expressions;

namespace Application.Common.Filtering;

/// <summary>
/// Extension methods for applying search filtering to IQueryable.
/// </summary>
public static class SearchExtensions
{
    /// <summary>
    /// Applies text search across multiple fields using OR logic.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="query">The query to filter.</param>
    /// <param name="configuration">The search configuration.</param>
    /// <param name="searchTerm">The search term.</param>
    /// <returns>The filtered query.</returns>
    public static IQueryable<TEntity> ApplySearch<TEntity>(
        this IQueryable<TEntity> query,
        SearchConfiguration<TEntity> configuration,
        string? searchTerm) where TEntity : class
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return query;
        }

        IReadOnlyList<Expression<Func<TEntity, string>>> expressions = configuration.GetSearchExpressions();
        if (expressions.Count == 0)
        {
            return query;
        }

        string normalizedSearchTerm = searchTerm.ToUpperInvariant();

        // Build combined OR expression
        ParameterExpression parameter = Expression.Parameter(typeof(TEntity), "e");
        Expression? combinedExpression = null;

        foreach (Expression<Func<TEntity, string>> searchExpression in expressions)
        {
            // Create: e.Field.ToUpper().Contains(searchTerm)
            Expression memberExpression = ReplacementVisitor.Replace(
                searchExpression.Body,
                searchExpression.Parameters[0],
                parameter);

            MethodCallExpression toUpperCall = Expression.Call(
                memberExpression,
                typeof(string).GetMethod(nameof(string.ToUpper), Type.EmptyTypes)!);

            MethodCallExpression containsCall = Expression.Call(
                toUpperCall,
                typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!,
                Expression.Constant(normalizedSearchTerm));

            combinedExpression = combinedExpression is null
                ? containsCall
                : Expression.OrElse(combinedExpression, containsCall);
        }

        if (combinedExpression is null)
        {
            return query;
        }

        // Type is apparent from Lambda<T>() method
        var lambda = Expression.Lambda<Func<TEntity, bool>>(combinedExpression, parameter);
        return query.Where(lambda);
    }

    /// <summary>
    /// Helper class to replace parameter in expression.
    /// </summary>
    private sealed class ReplacementVisitor : ExpressionVisitor
    {
        private readonly Expression _oldValue;
        private readonly Expression _newValue;

        private ReplacementVisitor(Expression oldValue, Expression newValue)
        {
            _oldValue = oldValue;
            _newValue = newValue;
        }

        public static Expression Replace(Expression expression, Expression oldValue, Expression newValue)
        {
            return new ReplacementVisitor(oldValue, newValue).Visit(expression);
        }

        public override Expression Visit(Expression? node)
        {
            return node == _oldValue ? _newValue : base.Visit(node)!;
        }
    }
}
