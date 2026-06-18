using Domain.Permissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Permissions;

internal sealed class DataMaskingPolicyConfiguration : IEntityTypeConfiguration<DataMaskingPolicy>
{
    public void Configure(EntityTypeBuilder<DataMaskingPolicy> builder)
    {
        builder.ToTable("data_masking_policies");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.RoleId)
            .IsRequired();

        builder.Property(p => p.VisibleFields)
            .IsRequired();

        builder.Property(p => p.PhoneMask)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue(DataMaskingPolicy.TextMask);

        builder.Property(p => p.EmailMaskValue)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue(DataMaskingPolicy.EmailMask);

        builder.Property(p => p.FinancialMaskValue)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue(DataMaskingPolicy.FinancialMask);

        builder.HasIndex(p => p.RoleId)
            .IsUnique();

        builder.Ignore(p => p.DomainEvents);
    }
}
