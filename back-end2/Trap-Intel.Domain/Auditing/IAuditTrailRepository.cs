namespace Trap_Intel.Domain.Auditing
{
    /// <summary>
    /// Repository interface for AuditTrail aggregate root.
    /// Abstracts data access for audit logs.
    /// </summary>
    public interface IAuditTrailRepository
    {
        /// <summary>
        /// Get audit entry by ID.
        /// </summary>
        Task<AuditTrail?> GetByIdAsync(Guid auditTrailId);

        /// <summary>
        /// Get all audit entries for a resource.
        /// </summary>
        Task<IReadOnlyList<AuditTrail>> GetByResourceAsync(
            Guid organizationId,
            Guid resourceId,
            int pageNumber = 1,
            int pageSize = 50);

        /// <summary>
        /// Get all audit entries for a resource type in an organization.
        /// </summary>
        Task<IReadOnlyList<AuditTrail>> GetByResourceTypeAsync(
            Guid organizationId,
            AuditResourceType resourceType,
            int pageNumber = 1,
            int pageSize = 50);

        /// <summary>
        /// Get audit entries by user action.
        /// </summary>
        Task<IReadOnlyList<AuditTrail>> GetByUserAsync(
            Guid organizationId,
            Guid userId,
            int pageNumber = 1,
            int pageSize = 50);

        /// <summary>
        /// Get audit entries by action type.
        /// </summary>
        Task<IReadOnlyList<AuditTrail>> GetByActionAsync(
            Guid organizationId,
            AuditAction action,
            int pageNumber = 1,
            int pageSize = 50);

        /// <summary>
        /// Get audit entries by severity.
        /// </summary>
        Task<IReadOnlyList<AuditTrail>> GetBySeverityAsync(
            Guid organizationId,
            AuditSeverity severity,
            int pageNumber = 1,
            int pageSize = 50);

        /// <summary>
        /// Get audit entries within a date range.
        /// </summary>
        Task<IReadOnlyList<AuditTrail>> GetByDateRangeAsync(
            Guid organizationId,
            DateTime startDate,
            DateTime endDate,
            int pageNumber = 1,
            int pageSize = 50);

        /// <summary>
        /// Get audit entries filtered by Compliance Standard.
        /// </summary>
        Task<IReadOnlyList<AuditTrail>> GetByComplianceStandardAsync(
            Guid organizationId,
            ComplianceStandard standard,
            int pageNumber = 1,
            int pageSize = 50);

        /// <summary>
        /// Get audit entries by IP Address (for forensic analysis).
        /// </summary>
        Task<IReadOnlyList<AuditTrail>> GetByIpAddressAsync(
            Guid organizationId,
            string ipAddress,
            int pageNumber = 1,
            int pageSize = 50);

        /// <summary>
        /// Search audit logs with multiple optional filters (Complex Projection).
        /// </summary>
        Task<IReadOnlyList<AuditTrail>> SearchAsync(
            Guid organizationId,
            Guid? userId = null,
            AuditAction? action = null,
            AuditResourceType? resourceType = null,
            AuditSeverity? severity = null,
            string? ipAddress = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            ComplianceStandard? standard = null,
            bool includeArchived = false,
            int pageNumber = 1,
            int pageSize = 50,
            AuditTrailSortBy sortBy = AuditTrailSortBy.Timestamp,
            AuditTrailSortDirection sortDirection = AuditTrailSortDirection.Desc,
            bool? isAcknowledged = null,
            string? reasonContains = null);

        Task<(IReadOnlyList<AuditTrail> Items, int TotalCount)> SearchPagedAsync(
            Guid organizationId,
            Guid? userId = null,
            AuditAction? action = null,
            AuditResourceType? resourceType = null,
            AuditSeverity? severity = null,
            string? ipAddress = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            ComplianceStandard? standard = null,
            bool includeArchived = false,
            int pageNumber = 1,
            int pageSize = 50,
            AuditTrailSortBy sortBy = AuditTrailSortBy.Timestamp,
            AuditTrailSortDirection sortDirection = AuditTrailSortDirection.Desc,
            bool? isAcknowledged = null,
            string? reasonContains = null);

        /// <summary>
        /// Get aggregate summary metrics for audit logs.
        /// </summary>
        Task<AuditLogsSummarySnapshot> GetSummaryAsync(
            Guid organizationId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            bool includeArchived = true,
            int top = 5);

        /// <summary>
        /// Get critical audit entries (errors and critical severity).
        /// </summary>
        Task<IReadOnlyList<AuditTrail>> GetCriticalEntriesAsync(
            Guid organizationId,
            int pageNumber = 1,
            int pageSize = 50);

        Task<(IReadOnlyList<AuditTrail> Items, int TotalCount)> GetCriticalEntriesPagedAsync(
            Guid organizationId,
            int pageNumber = 1,
            int pageSize = 50,
            string? reasonContains = null,
            AuditTrailSortBy sortBy = AuditTrailSortBy.Timestamp,
            AuditTrailSortDirection sortDirection = AuditTrailSortDirection.Desc);

        /// <summary>
        /// Count unacknowledged critical entries for dashboard.
        /// </summary>
        Task<int> CountUnacknowledgedCriticalEntriesAsync(Guid organizationId);

        /// <summary>
        /// Get expired audit entries (for deletion).
        /// </summary>
        Task<IReadOnlyList<AuditTrail>> GetExpiredEntriesAsync(int pageSize = 100);

        /// <summary>
        /// Get entries ready for archiving.
        /// </summary>
        Task<IReadOnlyList<AuditTrail>> GetEntriesToArchiveAsync(int archiveAfterDays, int pageSize = 100);

        /// <summary>
        /// Count total audit entries for an organization.
        /// </summary>
        Task<int> CountByOrganizationAsync(Guid organizationId);

        /// <summary>
        /// Add new audit entry.
        /// </summary>
        Task AddAsync(AuditTrail auditTrail);

        /// <summary>
        /// Add multiple audit entries in batch.
        /// </summary>
        Task AddBatchAsync(IEnumerable<AuditTrail> auditTrails);

        /// <summary>
        /// Update an existing audit entry (e.g., to acknowledge).
        /// </summary>
        Task UpdateAsync(AuditTrail auditTrail);

        /// <summary>
        /// Delete expired audit entries.
        /// </summary>
        Task<int> DeleteExpiredEntriesAsync();

        /// <summary>
        /// Archive audit entries older than specified days.
        /// </summary>
        Task<int> ArchiveOlderThanAsync(int days);

        /// <summary>
        /// Delete audit entries older than specified days.
        /// </summary>
        Task<int> DeleteOlderThanAsync(int days);
    }
}
