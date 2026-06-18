using Trap_Intel.Domain.Dashboards.Enums;

namespace Trap_Intel.Domain.Dashboards;

/// <summary>
/// Repository interface for DashboardView aggregate.
/// </summary>
public interface IDashboardViewRepository
{
    /// <summary>
    /// Get dashboard by ID.
    /// </summary>
    Task<DashboardView?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all dashboards for a user (owned + shared).
    /// </summary>
    Task<IReadOnlyList<DashboardView>> GetByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get dashboards owned by a user.
    /// </summary>
    Task<IReadOnlyList<DashboardView>> GetOwnedByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get dashboards shared with a user.
    /// </summary>
    Task<IReadOnlyList<DashboardView>> GetSharedWithUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get default dashboard for a user.
    /// </summary>
    Task<DashboardView?> GetDefaultByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get dashboards by type for a user.
    /// </summary>
    Task<IReadOnlyList<DashboardView>> GetByTypeAsync(
        Guid userId,
        DashboardType type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all dashboards in an organization (for admin).
    /// </summary>
    Task<IReadOnlyList<DashboardView>> GetByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recently viewed dashboards for a user.
    /// </summary>
    Task<IReadOnlyList<DashboardView>> GetRecentlyViewedAsync(
        Guid userId,
        int count = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Count dashboards owned by a user.
    /// </summary>
    Task<int> CountByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user has a default dashboard.
    /// </summary>
    Task<bool> HasDefaultAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if dashboard name exists for user.
    /// </summary>
    Task<bool> ExistsByNameAsync(
        Guid userId,
        string name,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add new dashboard.
    /// </summary>
    Task AddAsync(DashboardView dashboard, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing dashboard.
    /// </summary>
    Task UpdateAsync(DashboardView dashboard, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete dashboard.
    /// </summary>
    Task DeleteAsync(DashboardView dashboard, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unset default for all user's dashboards (when setting new default).
    /// </summary>
    Task UnsetDefaultForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
