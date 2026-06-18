using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Trap_Intel.Domain.Shared
{
    /// <summary>
    /// Base specification pattern supporting filtering, ordering, and pagination.
    /// Uses projections to avoid N+1 queries instead of eager loading.
    /// This is the foundation for building type-safe query objects.
    /// </summary>
    public abstract class Specification<T>
    {
        // Filtering
        public Expression<Func<T, bool>>? Criteria { get; protected set; }

        // Ordering
        public Expression<Func<T, object>>? OrderBy { get; private set; }
        public Expression<Func<T, object>>? OrderByDescending { get; private set; }

        // Pagination
        public int Take { get; private set; }
        public int Skip { get; private set; }
        public bool IsPagingEnabled { get; private set; }

        /// <summary>
        /// Applies pagination to the specification.
        /// </summary>
        protected void ApplyPaging(int skip, int take)
        {
            Skip = skip;
            Take = take;
            IsPagingEnabled = true;
        }

        /// <summary>
        /// Applies ascending order by the given expression.
        /// </summary>
        protected void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
        {
            OrderBy = orderByExpression;
        }

        /// <summary>
        /// Applies descending order by the given expression.
        /// </summary>
        protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
        {
            OrderByDescending = orderByDescendingExpression;
        }
    }

    /// <summary>
    /// Specification with projection support for mapping to DTOs.
    /// This avoids N+1 query problems by using Select() at the database layer.
    /// Always prefer this over Specification&lt;T&gt; when you need to transform entities to DTOs.
    /// </summary>
    public abstract class Specification<T, TResult> : Specification<T>
    {
        /// <summary>
        /// Gets the projection expression to map from entity T to result TResult.
        /// This ensures only required data is selected from the database.
        /// </summary>
        public Expression<Func<T, TResult>>? Projection { get; protected set; }

        /// <summary>
        /// Applies a projection to map entities to a different type (typically a DTO).
        /// This is executed at the database level, improving performance.
        /// </summary>
        protected void ApplyProjection(Expression<Func<T, TResult>> projection)
        {
            Projection = projection;
        }
    }
}
