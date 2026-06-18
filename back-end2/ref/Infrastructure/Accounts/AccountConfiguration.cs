using Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Accounts;

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Industry)
            .HasMaxLength(100);

        builder.Property(a => a.Website)
            .HasMaxLength(500);

        builder.Property(a => a.Phone)
            .HasMaxLength(20);

        builder.Property(a => a.Address)
            .HasMaxLength(500);

        builder.Property(a => a.TaxNumber)
            .HasMaxLength(50);

        builder.Property(a => a.InternalNotes)
            .HasMaxLength(2000);

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.IsActive)
            .IsRequired();

        builder.HasIndex(a => a.Name);

        builder.HasIndex(a => a.IsActive);

        builder.HasMany(a => a.Contacts)
            .WithOne(c => c.Account)
            .HasForeignKey(c => c.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Settings)
            .WithOne(s => s.Account)
            .HasForeignKey<AccountSettings>(s => s.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(a => a.DomainEvents);
    }
}
