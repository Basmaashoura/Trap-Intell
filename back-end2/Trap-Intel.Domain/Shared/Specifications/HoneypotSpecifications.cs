using System;
using System.Linq.Expressions;
using Trap_Intel.Domain.Honeypots;

namespace Trap_Intel.Domain.Shared.Specifications
{
    /// <summary>
    /// Specifications for Honeypot aggregate.
    /// These implement the Specification pattern for complex queries.
    /// 
    /// NOTE: Projection specifications with DTOs belong in Application layer.
    /// Domain layer contains only pure filtering/ordering specifications.
    /// </summary>
    
    /// <summary>
    /// Specification to get active honeypots for an organization.
    /// </summary>
    public class ActiveHoneypotsForOrganizationSpec : Specification<Honeypot>
    {
        public ActiveHoneypotsForOrganizationSpec(Guid organizationId)
        {
            Criteria = h => h.OrganizationId == organizationId && h.Status == HoneypotStatus.Active;
            ApplyOrderByDescending(h => h.CreatedAt);
        }
    }

    /// <summary>
    /// Specification to get honeypots by status with pagination.
    /// </summary>
    public class HoneypotsByStatusSpec : Specification<Honeypot>
    {
        public HoneypotsByStatusSpec(
            Guid organizationId,
            HoneypotStatus status,
            int pageNumber = 1,
            int pageSize = 20)
        {
            Criteria = h => h.OrganizationId == organizationId && h.Status == status;
            ApplyOrderByDescending(h => h.UpdatedAt);
            ApplyPaging((pageNumber - 1) * pageSize, pageSize);
        }
    }

    /// <summary>
    /// Specification to get honeypots with health issues.
    /// </summary>
    public class UnhealthyHoneypotsSpec : Specification<Honeypot>
    {
        public UnhealthyHoneypotsSpec(Guid organizationId)
        {
            Criteria = h => h.OrganizationId == organizationId 
                         && (h.Status == HoneypotStatus.Error || h.Health.Status == HoneypotHealthStatus.Unhealthy);
            ApplyOrderByDescending(h => h.LastHeartbeat);
        }
    }

    /// <summary>
    /// Specification to search honeypots by name.
    /// </summary>
    public class HoneypotsByNameSpec : Specification<Honeypot>
    {
        public HoneypotsByNameSpec(Guid organizationId, string searchTerm)
        {
            Criteria = h => h.OrganizationId == organizationId 
                         && h.Name.Contains(searchTerm);
            ApplyOrderBy(h => h.Name);
        }
    }

    /// <summary>
    /// Specification to get active and healthy honeypots by type.
    /// </summary>
    public class ActiveHealthyHoneypotsByTypeSpec : Specification<Honeypot>
    {
        public ActiveHealthyHoneypotsByTypeSpec(
            Guid organizationId,
            HoneypotType type)
        {
            Criteria = h => h.OrganizationId == organizationId
                         && h.Status == HoneypotStatus.Active
                         && h.Type == type
                         && h.Health.Status == HoneypotHealthStatus.Healthy;
            
            ApplyOrderBy(h => h.Name);
        }
    }
}
