using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using Trap_Intel.Domain.Recommendations;

namespace Trap_Intel.Infrastructure.Persistence.Configurations.Recommendations;

public class AIRecommendationConfiguration : IEntityTypeConfiguration<AIRecommendation>
{
    public void Configure(EntityTypeBuilder<AIRecommendation> builder)
    {
        builder.ToTable("ai_recommendations");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(r => r.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.Property(r => r.UserId).HasColumnName("user_id");
        builder.Property(r => r.DashboardViewId).HasColumnName("dashboard_view_id");

        // Enums
        builder.Property(r => r.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.Priority)
            .HasColumnName("priority")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.Category)
            .HasColumnName("category")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Value Objects as owned types
        builder.OwnsOne(r => r.Title, title =>
        {
            title.Property(t => t.Value).HasColumnName("title").HasMaxLength(200).IsRequired();
        });

        builder.OwnsOne(r => r.Description, desc =>
        {
            desc.Property(d => d.Value).HasColumnName("description").HasMaxLength(2000).IsRequired();
        });

        builder.OwnsOne(r => r.ConfidenceScore, conf =>
        {
            conf.Property(c => c.Value).HasColumnName("confidence_score").HasPrecision(5, 2).IsRequired();
            conf.Ignore(c => c.IsHighConfidence);
            conf.Ignore(c => c.IsMediumConfidence);
            conf.Ignore(c => c.IsLowConfidence);
        });

        builder.OwnsOne(r => r.ImpactScore, impact =>
        {
            impact.Property(i => i.Value).HasColumnName("impact_score").HasPrecision(5, 2).IsRequired();
            impact.Ignore(i => i.IsHighImpact);
            impact.Ignore(i => i.IsMediumImpact);
            impact.Ignore(i => i.IsLowImpact);
        });

        // Actions as JSONB
        builder.Property(r => r.Actions)
            .HasColumnName("actions")
            .HasColumnType("jsonb")
            .HasConversion(
                v => SerializeActions(v),
                v => DeserializeActions(v),
                new ValueComparer<RecommendationActions>(
                    (a1, a2) => a1 != null && a2 != null && a1.Steps.SequenceEqual(a2.Steps),
                    a => a.Steps.Aggregate(0, (hash, step) => HashCode.Combine(hash, step.GetHashCode())),
                    a => a));

        // Other properties
        builder.Property(r => r.ExpiresAt).HasColumnName("expires_at");
        builder.Property(r => r.TriggerEvent).HasColumnName("trigger_event").HasMaxLength(500);
        builder.Property(r => r.AcceptedAt).HasColumnName("accepted_at");
        builder.Property(r => r.AcceptedBy).HasColumnName("accepted_by");
        builder.Property(r => r.AcceptanceNotes).HasColumnName("acceptance_notes").HasMaxLength(1000);
        builder.Property(r => r.RejectedAt).HasColumnName("rejected_at");
        builder.Property(r => r.RejectedBy).HasColumnName("rejected_by");
        builder.Property(r => r.RejectionReason).HasColumnName("rejection_reason").HasMaxLength(1000);
        builder.Property(r => r.ImplementationStartedAt).HasColumnName("implementation_started_at");
        builder.Property(r => r.ImplementationTargetDate).HasColumnName("implementation_target_date");
        builder.Property(r => r.ImplementedAt).HasColumnName("implemented_at");
        builder.Property(r => r.ImplementedBy).HasColumnName("implemented_by");
        builder.Property(r => r.ImplementationNotes).HasColumnName("implementation_notes").HasMaxLength(2000);
        builder.Property(r => r.FailedAt).HasColumnName("failed_at");
        builder.Property(r => r.FailedBy).HasColumnName("failed_by");
        builder.Property(r => r.FailureMessage).HasColumnName("failure_message").HasMaxLength(2000);
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // Indexes
        builder.HasIndex(r => r.OrganizationId).HasDatabaseName("ix_ai_recommendations_organization_id");
        builder.HasIndex(r => r.UserId).HasDatabaseName("ix_ai_recommendations_user_id");
        builder.HasIndex(r => r.Status).HasDatabaseName("ix_ai_recommendations_status");
        builder.HasIndex(r => r.Priority).HasDatabaseName("ix_ai_recommendations_priority");
        builder.HasIndex(r => r.Type).HasDatabaseName("ix_ai_recommendations_type");
        builder.HasIndex(r => r.CreatedAt).HasDatabaseName("ix_ai_recommendations_created_at");
        builder.HasIndex(r => new { r.OrganizationId, r.Status }).HasDatabaseName("ix_ai_recommendations_org_status");
        builder.HasIndex(r => new { r.OrganizationId, r.Priority, r.Status }).HasDatabaseName("ix_ai_recommendations_org_priority_status");
    }

    private static string SerializeActions(RecommendationActions actions)
    {
        if (actions == null || actions.Steps.Count == 0)
            return "[]";

        var list = actions.Steps.Select(s => new
        {
            s.Order,
            s.Title,
            s.Description,
            s.Command,
            s.LinkToDocumentation
        });
        return JsonSerializer.Serialize(list);
    }

    private static RecommendationActions DeserializeActions(string json)
    {
        try
        {
            if (string.IsNullOrEmpty(json) || json == "[]")
                return RecommendationActions.Empty();

            var elements = JsonSerializer.Deserialize<List<JsonElement>>(json);
            if (elements == null) return RecommendationActions.Empty();

            var steps = new List<ActionStep>();
            foreach (var e in elements)
            {
                var order = e.GetProperty("Order").GetInt32();
                var title = e.GetProperty("Title").GetString() ?? "";
                var description = e.GetProperty("Description").GetString() ?? "";
                var command = e.TryGetProperty("Command", out var cmd) && cmd.ValueKind != JsonValueKind.Null ? cmd.GetString() : null;
                var link = e.TryGetProperty("LinkToDocumentation", out var l) && l.ValueKind != JsonValueKind.Null ? l.GetString() : null;

                var stepResult = ActionStep.Create(order, title, description, command, link);
                if (stepResult.IsSuccess)
                    steps.Add(stepResult.Value);
            }

            var result = RecommendationActions.Create(steps);
            return result.IsSuccess ? result.Value : RecommendationActions.Empty();
        }
        catch { return RecommendationActions.Empty(); }
    }
}
