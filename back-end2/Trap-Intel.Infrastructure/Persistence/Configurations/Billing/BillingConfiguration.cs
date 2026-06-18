using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using Trap_Intel.Domain.Billing;

namespace Trap_Intel.Infrastructure.Persistence.Configurations.Billing;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(i => i.SubscriptionId).HasColumnName("subscription_id").IsRequired();
        builder.Property(i => i.OrganizationId).HasColumnName("organization_id").IsRequired();

        builder.Property(i => i.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(i => i.IssueDate).HasColumnName("issue_date");
        builder.Property(i => i.DueDate).HasColumnName("due_date");
        builder.Property(i => i.PaymentId).HasColumnName("payment_id");
        builder.Property(i => i.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(i => i.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // InvoiceNumber (owned)
        builder.OwnsOne(i => i.InvoiceNumber, num =>
        {
            num.Property(n => n.Value).HasColumnName("invoice_number").HasMaxLength(50).IsRequired();
        });

        // BillingPeriod (owned)
        builder.OwnsOne(i => i.BillingPeriod, period =>
        {
            period.Property(p => p.StartDate).HasColumnName("billing_period_start").IsRequired();
            period.Property(p => p.EndDate).HasColumnName("billing_period_end").IsRequired();
            period.Ignore(p => p.DaysInPeriod);
        });

        // InvoiceAmount (owned)
        builder.OwnsOne(i => i.Amount, amount =>
        {
            amount.Property(a => a.BaseAmount).HasColumnName("base_amount").HasPrecision(18, 2).IsRequired();
            amount.Property(a => a.OverageAmount).HasColumnName("overage_amount").HasPrecision(18, 2).HasDefaultValue(0);
            amount.Property(a => a.TaxAmount).HasColumnName("tax_amount").HasPrecision(18, 2).HasDefaultValue(0);
            amount.Property(a => a.Discount).HasColumnName("discount_amount").HasPrecision(18, 2).HasDefaultValue(0);
            amount.Property(a => a.Currency).HasColumnName("currency").HasMaxLength(3).HasDefaultValue("USD");
            amount.Ignore(a => a.TotalAmount);
        });

        // UsageDetails (owned)
        builder.OwnsOne(i => i.UsageDetails, usage =>
        {
            usage.Property(u => u.HoneypotsUsed).HasColumnName("usage_honeypots").HasDefaultValue(0);
            usage.Property(u => u.StorageUsedGb).HasColumnName("usage_storage_gb").HasPrecision(18, 4).HasDefaultValue(0);
            usage.Property(u => u.OverageCharges).HasColumnName("usage_overage_charges").HasPrecision(18, 2).HasDefaultValue(0);
        });

        // TaxInfo (owned)
        builder.OwnsOne(i => i.TaxInfo, tax =>
        {
            tax.Property(t => t.TaxId).HasColumnName("tax_id").HasMaxLength(50);
            tax.Property(t => t.TaxRate).HasColumnName("tax_rate").HasPrecision(5, 4).HasDefaultValue(0);
        });

        // Notes as JSONB
        builder.Property(i => i.Notes)
            .HasColumnName("notes")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>(),
                new ValueComparer<IReadOnlyList<string>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, s) => HashCode.Combine(a, s.GetHashCode())),
                    c => c.ToList()));

        // Indexes
        builder.HasIndex(i => i.SubscriptionId).HasDatabaseName("ix_invoices_subscription_id");
        builder.HasIndex(i => i.OrganizationId).HasDatabaseName("ix_invoices_organization_id");
        builder.HasIndex(i => i.Status).HasDatabaseName("ix_invoices_status");
        builder.HasIndex(i => i.DueDate).HasDatabaseName("ix_invoices_due_date");
        builder.HasIndex(i => new { i.OrganizationId, i.Status }).HasDatabaseName("ix_invoices_org_status");
    }
}

public class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        builder.ToTable("payment_methods");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(p => p.OrganizationId).HasColumnName("organization_id").IsRequired();

        builder.Property(p => p.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.IsDefault).HasColumnName("is_default").HasDefaultValue(false);

        // PaymentMethodDetails (owned)
        builder.OwnsOne(p => p.Details, details =>
        {
            details.Property(d => d.LastFourDigits).HasColumnName("last_four_digits").HasMaxLength(4);
            details.Property(d => d.CardBrand).HasColumnName("card_brand").HasMaxLength(50);
            details.Property(d => d.PaymentProcessor).HasColumnName("payment_processor").HasMaxLength(50);
            details.Property(d => d.Token).HasColumnName("token").HasMaxLength(500);
            details.Property(d => d.ExpiresAt).HasColumnName("expires_at");
            details.Property(d => d.BillingContactEmail).HasColumnName("billing_contact_email").HasMaxLength(255);
            details.Ignore(d => d.IsExpired);
        });

        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // Ignore computed properties
        builder.Ignore(p => p.IsExpired);
        builder.Ignore(p => p.IsUsable);
        builder.Ignore(p => p.IsCreditCard);
        builder.Ignore(p => p.IsDebitCard);
        builder.Ignore(p => p.IsBankTransfer);
        builder.Ignore(p => p.IsPayPal);
        builder.Ignore(p => p.IsCrypto);
        builder.Ignore(p => p.IsACH);
        builder.Ignore(p => p.IsMobilePayment);

        // Indexes
        builder.HasIndex(p => p.OrganizationId).HasDatabaseName("ix_payment_methods_organization_id");
        builder.HasIndex(p => p.OrganizationId)
            .IsUnique()
            .HasFilter("is_default = true")
            .HasDatabaseName("ux_payment_methods_org_default");
    }
}
