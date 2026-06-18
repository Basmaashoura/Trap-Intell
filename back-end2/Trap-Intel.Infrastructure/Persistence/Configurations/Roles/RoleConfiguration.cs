using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using Trap_Intel.Domain.Roles;

namespace Trap_Intel.Infrastructure.Persistence.Configurations.Roles;

/// <summary>
/// EF Core configuration for the Role entity.
/// Uses JSON conversion for the Permissions collection to optimize storage and query performance.
/// </summary>
public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        // Primary Key
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever()
            .HasColumnName("id");

        // Basic Properties
        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("name");

        builder.Property(r => r.Description)
            .HasMaxLength(500)
            .HasColumnName("description");

        builder.Property(r => r.OrganizationId)
            .HasColumnName("organization_id");

        builder.Property(r => r.IsSystemRole)
            .IsRequired()
            .HasColumnName("is_system_role");

        builder.Property(r => r.IsActive)
            .IsRequired()
            .HasColumnName("is_active");

        builder.Property(r => r.IsDeleted)
            .IsRequired()
            .HasColumnName("is_deleted");

        builder.Property(r => r.DeletedAt)
            .HasColumnName("deleted_at");

        // Value Object / Collection conversion using JSON
        var permissionsProperty = builder.Property(r => r.Permissions)
            .HasColumnName("permissions")
            .HasColumnType("jsonb") // Optimize for Postgres Jsonb if used. Ignored safely by SQL Server.
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<HashSet<string>>(v, JsonSerializerOptions.Default) ?? new HashSet<string>());

        permissionsProperty.Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<IReadOnlyCollection<string>>(
                (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList().AsReadOnly()));

        permissionsProperty.Metadata.SetField("_permissions");
        permissionsProperty.Metadata.SetPropertyAccessMode(PropertyAccessMode.Field);

        // Auditable fields
        builder.Property<DateTime>("CreatedAt")
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property<DateTime?>("UpdatedAt")
            .HasColumnName("updated_at");

        // Indexes for performance
        // Combined index on OrganizationId and Name to enforce unique names per tenant.
        builder.HasIndex(r => new { r.OrganizationId, r.Name })
            .IsUnique()
            .HasFilter("is_deleted = false") // Only enforce on active roles
            .HasDatabaseName("ix_roles_org_name_unique");

        builder.HasIndex(r => r.IsSystemRole)
            .HasDatabaseName("ix_roles_is_system");

        // Permissions is a scalar JSON column backed by a field, not a navigation.
    }
}
