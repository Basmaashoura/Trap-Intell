using Microsoft.EntityFrameworkCore;
using Trap_Intel.Domain.Auditing;
using Trap_Intel.Infrastructure.Persistence;

namespace Trap_Intel.Infrastructure.Auditing.Repositories;

internal sealed class AuditTrailRepository : IAuditTrailRepository
{
    private readonly ApplicationDbContext _dbContext;

    public AuditTrailRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AuditTrail?> GetByIdAsync(Guid auditTrailId)
    {
        return await _dbContext.AuditTrails
            .FirstOrDefaultAsync(a => a.Id == auditTrailId);
    }

    public async Task<IReadOnlyList<AuditTrail>> GetByResourceAsync(Guid organizationId, Guid resourceId, int pageNumber = 1, int pageSize = 50)
    {
        return await _dbContext.AuditTrails
            .Where(a => a.OrganizationId == organizationId && a.ResourceId == resourceId)
            .OrderByDescending(a => a.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AuditTrail>> GetByResourceTypeAsync(Guid organizationId, AuditResourceType resourceType, int pageNumber = 1, int pageSize = 50)
    {
        return await _dbContext.AuditTrails
            .Where(a => a.OrganizationId == organizationId && a.ResourceType == resourceType)
            .OrderByDescending(a => a.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AuditTrail>> GetByUserAsync(Guid organizationId, Guid userId, int pageNumber = 1, int pageSize = 50)
    {
        return await _dbContext.AuditTrails
            .Where(a => a.OrganizationId == organizationId && a.UserId == userId)
            .OrderByDescending(a => a.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AuditTrail>> GetByActionAsync(Guid organizationId, AuditAction action, int pageNumber = 1, int pageSize = 50)
    {
        return await _dbContext.AuditTrails
            .Where(a => a.OrganizationId == organizationId && a.Action == action)
            .OrderByDescending(a => a.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AuditTrail>> GetBySeverityAsync(Guid organizationId, AuditSeverity severity, int pageNumber = 1, int pageSize = 50)
    {
        return await _dbContext.AuditTrails
            .Where(a => a.OrganizationId == organizationId && a.Severity == severity)
            .OrderByDescending(a => a.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AuditTrail>> GetByDateRangeAsync(Guid organizationId, DateTime startDate, DateTime endDate, int pageNumber = 1, int pageSize = 50)
    {
        return await _dbContext.AuditTrails
            .Where(a => a.OrganizationId == organizationId && a.Timestamp >= startDate && a.Timestamp <= endDate)
            .OrderByDescending(a => a.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public Task<IReadOnlyList<AuditTrail>> GetByComplianceStandardAsync(
        Guid organizationId,
        ComplianceStandard standard,
        int pageNumber = 1,
        int pageSize = 50)
    {
        // Using EF.Functions to search inside the JSONB compliance standards array
        // Fallback or exact syntax depends on postgresql/npgsql, 
        // string mapping allows a `Contains` mapping or `Like` fallback depending on provider.
        var targetStandard = standard.ToString();

        var result = _dbContext.AuditTrails
            .Where(a => a.OrganizationId == organizationId)
            .AsEnumerable() // In-memory fallback if JSON processing varies
            .Where(a => a.IsTaggedForCompliance(standard))
            .OrderByDescending(a => a.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult<IReadOnlyList<AuditTrail>>(result);
    }

    public async Task<IReadOnlyList<AuditTrail>> GetByIpAddressAsync(
        Guid organizationId, 
        string ipAddress, 
        int pageNumber = 1, 
        int pageSize = 50)
    {
        return await _dbContext.AuditTrails
            .Where(a => a.OrganizationId == organizationId && a.IpAddress == ipAddress)
            .OrderByDescending(a => a.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AuditTrail>> SearchAsync(
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
        string? reasonContains = null)
    {
        var (items, _) = await SearchPagedAsync(
            organizationId,
            userId,
            action,
            resourceType,
            severity,
            ipAddress,
            startDate,
            endDate,
            standard,
            includeArchived,
            pageNumber,
            pageSize,
            sortBy,
            sortDirection,
            isAcknowledged,
            reasonContains);

        return items;
    }

    public async Task<(IReadOnlyList<AuditTrail> Items, int TotalCount)> SearchPagedAsync(
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
        string? reasonContains = null)
    {
        var normalizedPageNumber = pageNumber < 1 ? 1 : pageNumber;
        var normalizedPageSize = pageSize < 1 ? 50 : pageSize;

        var query = BuildSearchQuery(
            organizationId,
            userId,
            action,
            resourceType,
            severity,
            ipAddress,
            startDate,
            endDate,
            includeArchived,
            isAcknowledged,
            reasonContains);

        var sortedQuery = ApplySorting(query, sortBy, sortDirection);

        if (standard.HasValue)
        {
            var standardValue = standard.Value;

            var matchingItems = sortedQuery
                .AsEnumerable()
                .Where(a => a.IsTaggedForCompliance(standardValue))
                .ToList();

            var totalCount = matchingItems.Count;
            var pagedItems = matchingItems
                .Skip((normalizedPageNumber - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .ToList();

            return (pagedItems, totalCount);
        }

        var dbTotalCount = await sortedQuery.CountAsync();
        var dbItems = await sortedQuery
            .Skip((normalizedPageNumber - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync();

        return (dbItems, dbTotalCount);
    }

    public async Task<AuditLogsSummarySnapshot> GetSummaryAsync(
        Guid organizationId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool includeArchived = true,
        int top = 5)
    {
        var topValue = top < 1 ? 5 : Math.Min(top, 20);

        var query = _dbContext.AuditTrails
            .AsNoTracking()
            .Where(a => a.OrganizationId == organizationId);

        if (!includeArchived)
            query = query.Where(a => !a.IsArchived);

        if (startDate.HasValue)
            query = query.Where(a => a.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.Timestamp <= endDate.Value);

        var totalEvents = await query.CountAsync();
        var acknowledgedEvents = await query.CountAsync(a => a.IsAcknowledged);
        var archivedEvents = await query.CountAsync(a => a.IsArchived);

        var severityRows = await query
            .GroupBy(a => a.Severity)
            .Select(g => new { Severity = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .ToListAsync();

        var actionRows = await query
            .GroupBy(a => a.Action)
            .Select(g => new { Action = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(topValue)
            .ToListAsync();

        var resourceRows = await query
            .GroupBy(a => a.ResourceType)
            .Select(g => new { ResourceType = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(topValue)
            .ToListAsync();

        return new AuditLogsSummarySnapshot(
            TotalEvents: totalEvents,
            AcknowledgedEvents: acknowledgedEvents,
            UnacknowledgedEvents: Math.Max(totalEvents - acknowledgedEvents, 0),
            ArchivedEvents: archivedEvents,
            EventsBySeverity: severityRows.Select(x => new AuditSeveritySummary(x.Severity, x.Count)).ToList(),
            TopActions: actionRows.Select(x => new AuditActionSummary(x.Action, x.Count)).ToList(),
            TopResourceTypes: resourceRows.Select(x => new AuditResourceTypeSummary(x.ResourceType, x.Count)).ToList()
        );
    }

    public async Task<IReadOnlyList<AuditTrail>> GetCriticalEntriesAsync(Guid organizationId, int pageNumber = 1, int pageSize = 50)
    {
        var (items, _) = await GetCriticalEntriesPagedAsync(organizationId, pageNumber, pageSize);
        return items;
    }

    public async Task<(IReadOnlyList<AuditTrail> Items, int TotalCount)> GetCriticalEntriesPagedAsync(
        Guid organizationId,
        int pageNumber = 1,
        int pageSize = 50,
        string? reasonContains = null,
        AuditTrailSortBy sortBy = AuditTrailSortBy.Timestamp,
        AuditTrailSortDirection sortDirection = AuditTrailSortDirection.Desc)
    {
        var normalizedPageNumber = pageNumber < 1 ? 1 : pageNumber;
        var normalizedPageSize = pageSize < 1 ? 50 : pageSize;

        var query = _dbContext.AuditTrails
            .AsNoTracking()
            .Where(a => a.OrganizationId == organizationId &&
                        (a.Severity == AuditSeverity.Critical || a.Severity == AuditSeverity.Warning));

        if (!string.IsNullOrWhiteSpace(reasonContains))
        {
            var pattern = $"%{reasonContains.Trim()}%";
            query = query.Where(a =>
                (a.Reason != null && EF.Functions.ILike(a.Reason, pattern)) ||
                (a.IpAddress != null && EF.Functions.ILike(a.IpAddress, pattern)) ||
                (a.UserAgent != null && EF.Functions.ILike(a.UserAgent, pattern)));
        }

        var sortedQuery = ApplySorting(query, sortBy, sortDirection);

        var totalCount = await sortedQuery.CountAsync();

        var items = await sortedQuery
            .Skip((normalizedPageNumber - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<AuditTrail>> GetExpiredEntriesAsync(int pageSize = 100)
    {
        var now = DateTime.UtcNow;
        return await _dbContext.AuditTrails // using manual Linq representation of Domain property ExtpirationDate logic
            .Where(a => now > a.Timestamp.AddDays(a.RetentionPeriodDays))
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AuditTrail>> GetEntriesToArchiveAsync(int archiveAfterDays, int pageSize = 100)
    {
        var archiveDate = DateTime.UtcNow.AddDays(-archiveAfterDays);
        return await _dbContext.AuditTrails
            .Where(a => !a.IsArchived && a.Timestamp < archiveDate)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountUnacknowledgedCriticalEntriesAsync(Guid organizationId)
    {
        return await _dbContext.AuditTrails
            .CountAsync(a => a.OrganizationId == organizationId 
                && a.Severity == AuditSeverity.Critical 
                && !a.IsAcknowledged);
    }

    public async Task<int> CountByOrganizationAsync(Guid organizationId)
    {
        return await _dbContext.AuditTrails
            .CountAsync(a => a.OrganizationId == organizationId);
    }

    public async Task AddAsync(AuditTrail auditTrail)
    {
        await _dbContext.AuditTrails.AddAsync(auditTrail);
    }

    public async Task AddBatchAsync(IEnumerable<AuditTrail> auditTrails)
    {
        await _dbContext.AuditTrails.AddRangeAsync(auditTrails);
    }

    public Task UpdateAsync(AuditTrail auditTrail)
    {
        _dbContext.AuditTrails.Update(auditTrail);
        return Task.CompletedTask;
    }

    public async Task<int> DeleteExpiredEntriesAsync()
    {
        var now = DateTime.UtcNow;
        var expiredTrails = await _dbContext.AuditTrails
            .Where(a => now > a.Timestamp.AddDays(a.RetentionPeriodDays))
            .ToListAsync();

        _dbContext.AuditTrails.RemoveRange(expiredTrails);
        return expiredTrails.Count;
    }

    public async Task<int> ArchiveOlderThanAsync(int days)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        var oldTrails = await _dbContext.AuditTrails
            .Where(a => !a.IsArchived && a.Timestamp < cutoffDate)
            .ToListAsync();

        foreach (var trail in oldTrails)
        {
            trail.Archive();
        }

        return oldTrails.Count;
    }

    public async Task<int> DeleteOlderThanAsync(int days)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        var oldTrails = await _dbContext.AuditTrails
            .Where(a => a.Timestamp < cutoffDate)
            .ToListAsync();

        _dbContext.AuditTrails.RemoveRange(oldTrails);
        return oldTrails.Count;
    }

    private IQueryable<AuditTrail> BuildSearchQuery(
        Guid organizationId,
        Guid? userId,
        AuditAction? action,
        AuditResourceType? resourceType,
        AuditSeverity? severity,
        string? ipAddress,
        DateTime? startDate,
        DateTime? endDate,
        bool includeArchived,
        bool? isAcknowledged,
        string? reasonContains)
    {
        var query = _dbContext.AuditTrails
            .AsNoTracking()
            .Where(a => a.OrganizationId == organizationId);

        if (!includeArchived)
        {
            query = query.Where(a => !a.IsArchived);
        }

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        if (action.HasValue)
            query = query.Where(a => a.Action == action.Value);

        if (resourceType.HasValue)
            query = query.Where(a => a.ResourceType == resourceType.Value);

        if (severity.HasValue)
            query = query.Where(a => a.Severity == severity.Value);

        if (!string.IsNullOrWhiteSpace(ipAddress))
            query = query.Where(a => a.IpAddress == ipAddress);

        if (startDate.HasValue)
            query = query.Where(a => a.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.Timestamp <= endDate.Value);

        if (isAcknowledged.HasValue)
            query = query.Where(a => a.IsAcknowledged == isAcknowledged.Value);

        if (!string.IsNullOrWhiteSpace(reasonContains))
        {
            var pattern = $"%{reasonContains.Trim()}%";
            query = query.Where(a =>
                (a.Reason != null && EF.Functions.ILike(a.Reason, pattern)) ||
                (a.IpAddress != null && EF.Functions.ILike(a.IpAddress, pattern)) ||
                (a.UserAgent != null && EF.Functions.ILike(a.UserAgent, pattern)));
        }

        return query;
    }

    private static IQueryable<AuditTrail> ApplySorting(
        IQueryable<AuditTrail> query,
        AuditTrailSortBy sortBy,
        AuditTrailSortDirection sortDirection)
    {
        return (sortBy, sortDirection) switch
        {
            (AuditTrailSortBy.Severity, AuditTrailSortDirection.Asc) => query.OrderBy(a => a.Severity).ThenByDescending(a => a.Timestamp),
            (AuditTrailSortBy.Severity, _) => query.OrderByDescending(a => a.Severity).ThenByDescending(a => a.Timestamp),
            (AuditTrailSortBy.Action, AuditTrailSortDirection.Asc) => query.OrderBy(a => a.Action).ThenByDescending(a => a.Timestamp),
            (AuditTrailSortBy.Action, _) => query.OrderByDescending(a => a.Action).ThenByDescending(a => a.Timestamp),
            (AuditTrailSortBy.ResourceType, AuditTrailSortDirection.Asc) => query.OrderBy(a => a.ResourceType).ThenByDescending(a => a.Timestamp),
            (AuditTrailSortBy.ResourceType, _) => query.OrderByDescending(a => a.ResourceType).ThenByDescending(a => a.Timestamp),
            (AuditTrailSortBy.Timestamp, AuditTrailSortDirection.Asc) => query.OrderBy(a => a.Timestamp),
            _ => query.OrderByDescending(a => a.Timestamp)
        };
    }
}
