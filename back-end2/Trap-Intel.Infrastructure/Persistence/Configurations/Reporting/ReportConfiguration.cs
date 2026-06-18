using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using Trap_Intel.Domain.Reporting;

namespace Trap_Intel.Infrastructure.Persistence.Configurations.Reporting;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("reports");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(r => r.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.Property(r => r.UserId).HasColumnName("user_id");
        builder.Property(r => r.SubscriptionId).HasColumnName("subscription_id");

        // Enums
        builder.Property(r => r.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.Format)
            .HasColumnName("format")
            .HasConversion<string>()
            .HasMaxLength(50);

        // Value Objects as owned types
        builder.OwnsOne(r => r.Title, title =>
        {
            title.Property(t => t.Value).HasColumnName("title").HasMaxLength(200).IsRequired();
        });

        builder.OwnsOne(r => r.Summary, summary =>
        {
            summary.Property(s => s.Value).HasColumnName("summary").HasMaxLength(2000).IsRequired();
        });

        // KPIs as JSONB
        builder.Property(r => r.KPIs)
            .HasColumnName("kpis")
            .HasColumnType("jsonb")
            .HasConversion(
                v => SerializeKPIs(v),
                v => DeserializeKPIs(v),
                new ValueComparer<KPICollection>(
                    (c1, c2) => c1 != null && c2 != null && c1.Items.SequenceEqual(c2.Items),
                    c => c.Items.Aggregate(0, (a, k) => HashCode.Combine(a, k.GetHashCode())),
                    c => c));

        // LogDetails as owned entity
        builder.OwnsOne(r => r.LogDetails, log =>
        {
            log.Property(l => l.TotalLogsAnalyzed).HasColumnName("log_total_analyzed").IsRequired();
            log.Property(l => l.CriticalEvents).HasColumnName("log_critical_events").HasDefaultValue(0);
            log.Property(l => l.WarningEvents).HasColumnName("log_warning_events").HasDefaultValue(0);
            log.Property(l => l.InfoEvents).HasColumnName("log_info_events").HasDefaultValue(0);
            log.Property(l => l.AnalysisStartTime).HasColumnName("log_analysis_start").IsRequired();
            log.Property(l => l.AnalysisEndTime).HasColumnName("log_analysis_end").IsRequired();
            log.Property(l => l.AnalysisDuration)
                .HasColumnName("log_analysis_duration_ms")
                .HasConversion(
                    v => (long)v.TotalMilliseconds,
                    v => TimeSpan.FromMilliseconds((double)v));
            log.Ignore(l => l.CriticalityScore);
            log.Ignore(l => l.WarningPercentage);
            log.Ignore(l => l.InfoPercentage);
        });

        // Recommendations as JSONB
        builder.Property(r => r.Recommendations)
            .HasColumnName("recommendations")
            .HasColumnType("jsonb")
            .HasConversion(
                v => SerializeRecommendations(v),
                v => DeserializeRecommendations(v),
                new ValueComparer<RecommendationCollection>(
                    (c1, c2) => c1 != null && c2 != null && c1.Items.SequenceEqual(c2.Items),
                    c => c.Items.Aggregate(0, (a, r) => HashCode.Combine(a, r.GetHashCode())),
                    c => c));

        // Timestamps
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // Indexes
        builder.HasIndex(r => r.OrganizationId).HasDatabaseName("ix_reports_organization_id");
        builder.HasIndex(r => r.UserId).HasDatabaseName("ix_reports_user_id");
        builder.HasIndex(r => r.Type).HasDatabaseName("ix_reports_type");
        builder.HasIndex(r => r.Status).HasDatabaseName("ix_reports_status");
        builder.HasIndex(r => r.CreatedAt).HasDatabaseName("ix_reports_created_at");
        builder.HasIndex(r => new { r.OrganizationId, r.Status }).HasDatabaseName("ix_reports_org_status");
    }

    private static string SerializeKPIs(KPICollection kpis)
    {
        if (kpis == null || kpis.Items.Count == 0)
            return "[]";

        var list = kpis.Items.Select(k => new
        {
            k.Name,
            k.Value,
            k.Unit,
            k.Threshold,
            Trend = k.Trend.ToString()
        });
        return JsonSerializer.Serialize(list);
    }

    private static KPICollection DeserializeKPIs(string json)
    {
        try
        {
            if (string.IsNullOrEmpty(json) || json == "[]")
            {
                // Return a minimal valid collection
                var defaultKpi = KPI.Create("Default", 0, "count").Value;
                return KPICollection.Create(new[] { defaultKpi }).Value;
            }

            var elements = JsonSerializer.Deserialize<List<JsonElement>>(json);
            if (elements == null || elements.Count == 0)
            {
                var defaultKpi = KPI.Create("Default", 0, "count").Value;
                return KPICollection.Create(new[] { defaultKpi }).Value;
            }

            var kpis = new List<KPI>();
            foreach (var e in elements)
            {
                var name = e.GetProperty("Name").GetString() ?? "";
                var value = e.GetProperty("Value").GetDecimal();
                var unit = e.GetProperty("Unit").GetString() ?? "count";
                var threshold = e.TryGetProperty("Threshold", out var th) && th.ValueKind != JsonValueKind.Null ? th.GetDecimal() : (decimal?)null;
                var trendStr = e.TryGetProperty("Trend", out var tr) ? tr.GetString() ?? "Stable" : "Stable";
                var trend = Enum.TryParse<KPITrend>(trendStr, out var t) ? t : KPITrend.Stable;

                var kpiResult = KPI.Create(name, value, unit, threshold, trend);
                if (kpiResult.IsSuccess)
                    kpis.Add(kpiResult.Value);
            }

            var result = KPICollection.Create(kpis);
            return result.IsSuccess ? result.Value : KPICollection.Create(new[] { KPI.Create("Default", 0, "count").Value }).Value;
        }
        catch
        {
            var defaultKpi = KPI.Create("Default", 0, "count").Value;
            return KPICollection.Create(new[] { defaultKpi }).Value;
        }
    }

    private static string SerializeRecommendations(RecommendationCollection recs)
    {
        if (recs == null || recs.Items.Count == 0)
            return "[]";

        var list = recs.Items.Select(r => new
        {
            r.Title,
            r.Description,
            Priority = r.Priority.ToString(),
            r.ActionItems,
            r.SuggestedImplementationDate
        });
        return JsonSerializer.Serialize(list);
    }

    private static RecommendationCollection DeserializeRecommendations(string json)
    {
        try
        {
            if (string.IsNullOrEmpty(json) || json == "[]")
                return RecommendationCollection.Create(Enumerable.Empty<Recommendation>()).Value;

            var elements = JsonSerializer.Deserialize<List<JsonElement>>(json);
            if (elements == null)
                return RecommendationCollection.Create(Enumerable.Empty<Recommendation>()).Value;

            var recs = new List<Recommendation>();
            foreach (var e in elements)
            {
                var title = e.GetProperty("Title").GetString() ?? "";
                var description = e.GetProperty("Description").GetString() ?? "";
                var priorityStr = e.TryGetProperty("Priority", out var p) ? p.GetString() ?? "Medium" : "Medium";
                var priority = Enum.TryParse<RecommendationPriority>(priorityStr, out var pr) ? pr : RecommendationPriority.Medium;
                var actionItems = e.TryGetProperty("ActionItems", out var ai) ? ai.GetString() ?? "" : "";
                var implDate = e.TryGetProperty("SuggestedImplementationDate", out var d) ? d.GetDateTime() : DateTime.UtcNow.AddDays(30);

                var recResult = Recommendation.Create(title, description, priority, actionItems, implDate);
                if (recResult.IsSuccess)
                    recs.Add(recResult.Value);
            }

            var result = RecommendationCollection.Create(recs);
            return result.IsSuccess ? result.Value : RecommendationCollection.Create(Enumerable.Empty<Recommendation>()).Value;
        }
        catch
        {
            return RecommendationCollection.Create(Enumerable.Empty<Recommendation>()).Value;
        }
    }
}

public class ReportTemplateConfiguration : IEntityTypeConfiguration<ReportTemplate>
{
    public void Configure(EntityTypeBuilder<ReportTemplate> builder)
    {
        builder.ToTable("report_templates");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(t => t.OrganizationId).HasColumnName("organization_id");
        builder.Property(t => t.CreatedBy).HasColumnName("created_by").IsRequired();

        builder.Property(t => t.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Value Objects
        builder.OwnsOne(t => t.Name, name =>
        {
            name.Property(n => n.Value).HasColumnName("name").HasMaxLength(100).IsRequired();
        });

        builder.OwnsOne(t => t.Guidelines, guidelines =>
        {
            guidelines.Property(g => g.Value).HasColumnName("guidelines").HasMaxLength(5000).IsRequired();
        });

        // Sections as JSONB
        builder.Property(t => t.Sections)
            .HasColumnName("sections")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v.Select(s => new { s.Id, s.Name, s.Description, s.Order }), (JsonSerializerOptions?)null),
                v => DeserializeSections(v),
                new ValueComparer<IReadOnlyList<TemplateSection>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, s) => HashCode.Combine(a, s.GetHashCode())),
                    c => c.ToList()));

        builder.Property(t => t.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // Indexes
        builder.HasIndex(t => t.OrganizationId).HasDatabaseName("ix_report_templates_organization_id");
        builder.HasIndex(t => t.Type).HasDatabaseName("ix_report_templates_type");
    }

    private static IReadOnlyList<TemplateSection> DeserializeSections(string json)
    {
        try
        {
            if (string.IsNullOrEmpty(json) || json == "[]")
                return new List<TemplateSection>();

            var elements = JsonSerializer.Deserialize<List<JsonElement>>(json);
            if (elements == null) return new List<TemplateSection>();

            var sections = new List<TemplateSection>();
            foreach (var e in elements)
            {
                var name = e.GetProperty("Name").GetString() ?? "";
                var description = e.GetProperty("Description").GetString() ?? "";
                var order = e.TryGetProperty("Order", out var o) ? o.GetInt32() : 0;

                var sectionResult = TemplateSection.Create(name, description, order);
                if (sectionResult.IsSuccess)
                    sections.Add(sectionResult.Value);
            }
            return sections;
        }
        catch { return new List<TemplateSection>(); }
    }
}

public class ReportExportConfiguration : IEntityTypeConfiguration<ReportExport>
{
    public void Configure(EntityTypeBuilder<ReportExport> builder)
    {
        builder.ToTable("report_exports");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(e => e.ReportId).HasColumnName("report_id").IsRequired();
        builder.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.Property(e => e.UserId).HasColumnName("user_id").IsRequired();

        builder.Property(e => e.Format)
            .HasColumnName("format")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.FileUrl).HasColumnName("file_url").HasMaxLength(2000);
        builder.Property(e => e.ExportDate).HasColumnName("export_date").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();

        // Indexes
        builder.HasIndex(e => e.ReportId).HasDatabaseName("ix_report_exports_report_id");
        builder.HasIndex(e => e.OrganizationId).HasDatabaseName("ix_report_exports_organization_id");
        builder.HasIndex(e => e.UserId).HasDatabaseName("ix_report_exports_user_id");
        builder.HasIndex(e => e.Status).HasDatabaseName("ix_report_exports_status");
        builder.HasIndex(e => new { e.ReportId, e.Status }).HasDatabaseName("ix_report_exports_report_status");
    }
}
