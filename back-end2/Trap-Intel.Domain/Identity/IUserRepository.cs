namespace Trap_Intel.Domain.Identity
{
    /// <summary>
    /// Repository interface for User aggregate root.
    /// Abstracts data access for users.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Get user by ID.
        /// </summary>
        Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get user by email address.
        /// </summary>
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get user by username.
        /// </summary>
        Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users in an organization.
        /// </summary>
        Task<IReadOnlyList<User>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get active users in an organization.
        /// </summary>
        Task<IReadOnlyList<User>> GetActiveByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get users by role in an organization.
        /// </summary>
        Task<IReadOnlyList<User>> GetByRoleAsync(Guid organizationId, Guid roleId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if email already exists.
        /// </summary>
        Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if username already exists.
        /// </summary>
        Task<bool> UserNameExistsAsync(string userName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Count organization admins.
        /// </summary>
        Task<int> CountAdminsByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add new user.
        /// </summary>
        Task AddAsync(User user, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update existing user.
        /// </summary>
        Task UpdateAsync(User user, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete user.
        /// </summary>
        Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
