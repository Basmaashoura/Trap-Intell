using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trap_Intel.Domain.Organizations;
using Trap_Intel.Domain.Shared;
using Trap_Intel.Infrastructure.Persistence.Converters;

namespace Trap_Intel.Infrastructure.Configuration.EntityConfigurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations");

        // Primary Key
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // Basic Properties
        builder.Property(o => o.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(o => o.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(o => o.Industry)
            .HasColumnName("industry")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(o => o.Size)
            .HasColumnName("size")
            .IsRequired();

        builder.Property(o => o.Website)
            .HasColumnName("website")
            .HasMaxLength(500);

        builder.Property(o => o.Tagline)
            .HasColumnName("tagline")
            .HasMaxLength(250);

        builder.Property(o => o.Description)
            .HasColumnName("description")
            .HasMaxLength(4000);

        builder.Property(o => o.SupportEmail)
            .HasColumnName("support_email")
            .HasMaxLength(254);

        builder.Property(o => o.SupportPhone)
            .HasColumnName("support_phone")
            .HasMaxLength(30);

        builder.Property(o => o.HeadquartersLocation)
            .HasColumnName("headquarters_location")
            .HasMaxLength(250);

        builder.Property(o => o.LinkedInUrl)
            .HasColumnName("linkedin_url")
            .HasMaxLength(500);

        builder.Property(o => o.XUrl)
            .HasColumnName("x_url")
            .HasMaxLength(500);

        builder.Property(o => o.LogoUrl)
            .HasColumnName("logo_url")
            .HasMaxLength(500);

        builder.Property(o => o.LogoPublicId)
            .HasColumnName("logo_public_id")
            .HasMaxLength(255);

        builder.Property(o => o.CoverImageUrl)
            .HasColumnName("cover_image_url")
            .HasMaxLength(500);

        builder.Property(o => o.CoverImagePublicId)
            .HasColumnName("cover_image_public_id")
            .HasMaxLength(255);

        builder.Property(o => o.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(o => o.ParentOrganizationId)
            .HasColumnName("parent_organization_id");

        // Timestamps
        builder.Property(o => o.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(o => o.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(o => o.ApprovedAt)
            .HasColumnName("approved_at");

        builder.Property(o => o.ApprovalNotes)
            .HasColumnName("approval_notes")
            .HasMaxLength(1000);

        builder.Property(o => o.ApprovedByUserId)
            .HasColumnName("approved_by_user_id");

        // Value Object: Domain (owned, converted to string)
        builder.Property(o => o.Domain)
            .HasColumnName("domain")
            .HasMaxLength(253)
            .HasConversion(new OrganizationDomainConverter())
            .IsRequired();

        // Value Object: TaxIdentifier (owned, converted to string)
        builder.Property(o => o.TaxId)
            .HasColumnName("tax_id")
            .HasMaxLength(50)
            .HasConversion(new TaxIdentifierConverter())
            .IsRequired();

        // Value Object: ContactInfo (owned entity - stored as separate columns)
        builder.OwnsOne(o => o.ContactInfo, contact =>
        {
            contact.Property(c => c.Email)
                .HasColumnName("contact_email")
                .HasMaxLength(254)
                .IsRequired();

            contact.Property(c => c.Phone)
                .HasColumnName("contact_phone")
                .HasMaxLength(20)
                .IsRequired();

            contact.Property(c => c.Website)
                .HasColumnName("contact_website")
                .HasMaxLength(500);
        });

        // Value Object: OrganizationSettings (owned entity - stored as separate columns)
        builder.OwnsOne(o => o.Settings, settings =>
        {
            settings.Property(s => s.AllowMultipleAddresses)
                .HasColumnName("settings_allow_multiple_addresses")
                .HasDefaultValue(true);

            settings.Property(s => s.RequireApprovalForMembers)
                .HasColumnName("settings_require_approval_for_members")
                .HasDefaultValue(false);

            settings.Property(s => s.MaximumMembers)
                .HasColumnName("settings_maximum_members")
                .HasDefaultValue(1000);

            settings.Property(s => s.EnableBilling)
                .HasColumnName("settings_enable_billing")
                .HasDefaultValue(true);

            settings.Property(s => s.EnableApiAccess)
                .HasColumnName("settings_enable_api_access")
                .HasDefaultValue(false);
        });

        // Navigation: Addresses (one-to-many)
        builder.HasMany(o => o.Addresses)
            .WithOne()
            .HasForeignKey("OrganizationId")
            .OnDelete(DeleteBehavior.Cascade);

        // Self-referencing relationship for parent organization
        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(o => o.ParentOrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(o => o.Name)
            .HasDatabaseName("ix_organizations_name");

        builder.HasIndex(o => o.Status)
            .HasDatabaseName("ix_organizations_status");

        builder.HasIndex(o => o.ParentOrganizationId)
            .HasDatabaseName("ix_organizations_parent_id");
    }
}

public class OrganizationAddressConfiguration : IEntityTypeConfiguration<OrganizationAddress>
{
    public void Configure(EntityTypeBuilder<OrganizationAddress> builder)
    {
        builder.ToTable("organization_addresses");

        // Primary Key - using composite key or generated id
        builder.HasKey("OrganizationId", "AddressType");

        builder.Property<Guid>("OrganizationId")
            .HasColumnName("organization_id");

        builder.Property(a => a.AddressType)
            .HasColumnName("address_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Value Object: Address (owned entity)
        builder.OwnsOne(a => a.Address, address =>
        {
            address.Property(ad => ad.Street)
                .HasColumnName("street")
                .HasMaxLength(200)
                .IsRequired();

            address.Property(ad => ad.City)
                .HasColumnName("city")
                .HasMaxLength(100)
                .IsRequired();

            address.Property(ad => ad.State)
                .HasColumnName("state")
                .HasMaxLength(50)
                .IsRequired();

            address.Property(ad => ad.PostalCode)
                .HasColumnName("postal_code")
                .HasMaxLength(20)
                .IsRequired();

            address.Property(ad => ad.Country)
                .HasColumnName("country")
                .HasMaxLength(100);
        });
    }
}
