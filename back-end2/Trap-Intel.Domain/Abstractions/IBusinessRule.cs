namespace Trap_Intel.Domain.Abstractions
{
    /// <summary>
    /// Base interface for all business rules in the domain.
    /// Business rules encapsulate complex validation and policy logic.
    /// </summary>
    public interface IBusinessRule
    {
        /// <summary>
        /// Gets the error that occurs when this rule is broken.
        /// </summary>
        Error Error { get; }

        /// <summary>
        /// Checks if the rule is satisfied.
        /// </summary>
        /// <returns>True if the rule is satisfied, false otherwise.</returns>
        bool IsSatisfied();
    }

    /// <summary>
    /// Async business rule interface for rules requiring async operations.
    /// </summary>
    public interface IAsyncBusinessRule
    {
        /// <summary>
        /// Gets the error that occurs when this rule is broken.
        /// </summary>
        Error Error { get; }

        /// <summary>
        /// Asynchronously checks if the rule is satisfied.
        /// </summary>
        /// <returns>True if the rule is satisfied, false otherwise.</returns>
        Task<bool> IsSatisfiedAsync(CancellationToken cancellationToken = default);
    }
}
