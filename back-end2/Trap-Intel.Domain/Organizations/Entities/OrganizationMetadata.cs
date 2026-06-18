using System;
using System.Collections.Generic;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Domain.Organizations
{
    /// <summary>
    /// Represents organization metadata tracking for enterprise features.
    /// </summary>
    public class OrganizationMetadata : Abstractions.Entity<Guid>
    {
        private OrganizationMetadata() { }

        public OrganizationMetadata(
            Guid organizationId,
            string? logo = null,
            string? description = null,
            string? customAttributes = null)
        {
            OrganizationId = organizationId;
            Logo = logo;
            Description = description;
            CustomAttributes = customAttributes;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public Guid OrganizationId { get; private set; }
        public string? Logo { get; private set; }
        public string? Description { get; private set; }
        public string? CustomAttributes { get; private set; } // JSON for future extensibility
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        public void UpdateLogo(string logoUrl)
        {
            Logo = logoUrl;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateDescription(string description)
        {
            Description = description;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
