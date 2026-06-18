using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Auditing;
using Trap_Intel.Domain.Invitations;
using Trap_Intel.Domain.Organizations;

namespace Trap_Intel.Infrastructure.Auditing.Interceptors;

public sealed class AuditingSaveInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditingSaveInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // Normally injected via DI, but interceptors inside DbContext require careful DI handling.
    // For simplicity of the scope, we will inject a resolver or directly use the tracker.

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        GenerateAuditTrailsForChanges(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        GenerateAuditTrailsForChanges(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void GenerateAuditTrailsForChanges(DbContext? dbContext)
    {
        if (dbContext == null) return;

        // Ensure we only look at proper domain entities
        var entries = dbContext.ChangeTracker.Entries().Where(e =>
            e.State == EntityState.Added ||
            e.State == EntityState.Modified ||
            e.State == EntityState.Deleted).ToList();

        if (!entries.Any()) return;

        // Skip entities that are already covered by dedicated domain-event audit handlers.
        var validEntries = entries.Where(e =>
            e.Entity is not AuditTrail &&
            e.Entity is not Organization &&
            e.Entity is not OrganizationInvitation).ToList();

        if (!validEntries.Any())
            return;

        // Extracting common audit properties (normally fetched from ICurrentUserService injecting IHttpContextAccessor)
        Guid? userId = GetPotentialUserId();
        Guid orgId = GetPotentialOrgId(validEntries);

        foreach (var entry in validEntries)
        {
            var entityName = entry.Entity.GetType().Name;

            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Create,
                EntityState.Modified => AuditAction.Update,
                EntityState.Deleted => AuditAction.Delete,
                _ => AuditAction.Update
            };

            // Attempt to resolve dynamic Resource values
            var resourceId = GetEntityId(entry.Entity);
            var resourceType = MapEntityToResourceType(entityName);

            if (resourceId == Guid.Empty || orgId == Guid.Empty)
                continue;

            // Compute changes for properties
            var auditChanges = new List<AuditChange>();

            if (action == AuditAction.Update)
            {
                foreach (var property in entry.Properties)
                {
                    if (property.IsModified && property.Metadata.Name != "UpdatedAt")
                    {
                        var originalVal = property.OriginalValue?.ToString();
                        var currentVal = property.CurrentValue?.ToString();

                        if (originalVal != currentVal)
                        {
                            var changeResult = AuditChange.Create(property.Metadata.Name, originalVal, currentVal);
                            if (changeResult.IsSuccess)
                            {
                                auditChanges.Add(changeResult.Value);
                            }
                        }
                    }
                }
            }

            // Only Audit updates that actually changed tracked properties, or inserts/deletes
            if (action == AuditAction.Update && auditChanges.Count == 0)
                continue;

            var auditTrailResult = AuditTrail.Create(
                orgId,
                userId,
                resourceType,
                resourceId,
                action,
                AuditSeverity.Info,
                $"Auto-audited {action} on {entityName}",
                null,
                null,
                retentionDays: 90
            );

            if (auditTrailResult.IsSuccess)
            {
                var trail = auditTrailResult.Value;
                foreach (var auditChange in auditChanges)
                {
                    trail.AddChange(auditChange.PropertyName, auditChange.OldValue, auditChange.NewValue);
                }

                // Push new trail into Context explicitly, so it gets saved implicitly with parent context execution
                dbContext.Add(trail);
            }
        }
    }

    private AuditResourceType MapEntityToResourceType(string entityName)
    {
        if (string.Equals(entityName, nameof(OrganizationInvitation), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(entityName, nameof(Organization), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(entityName, nameof(OrganizationAddress), StringComparison.OrdinalIgnoreCase))
        {
            return AuditResourceType.Organization;
        }

        if (string.Equals(entityName, "Honeypot", StringComparison.OrdinalIgnoreCase))
        {
            return AuditResourceType.HoneyPot;
        }

        return Enum.TryParse<AuditResourceType>(entityName, out var type) ? type : AuditResourceType.Settings;
    }

    private Guid GetEntityId(object entity)
    {
        var property = entity.GetType().GetProperty("Id");
        if (property != null && property.PropertyType == typeof(Guid))
        {
            return (Guid)(property.GetValue(entity) ?? Guid.Empty);
        }
        return Guid.Empty;
    }

    // In a real scenario, this would be injected via ICurrentUserService.
    // For DbContext interceptors, sometimes context variables fallback to Shadow Properties.
    private Guid? GetPotentialUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        return Guid.TryParse(userIdValue, out var userId)
            ? userId
            : null;
    }

    private Guid GetPotentialOrgId(List<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry> entries)
    {
        foreach (var entry in entries)
        {
            var property = entry.Entity.GetType().GetProperty("OrganizationId");
            if (property != null && property.PropertyType == typeof(Guid))
            {
                var v = property.GetValue(entry.Entity);
                if (v != null && (Guid)v != Guid.Empty)
                    return (Guid)v;
            }
        }
        return Guid.Empty; // Requires strict tenant fallback
    }
}
