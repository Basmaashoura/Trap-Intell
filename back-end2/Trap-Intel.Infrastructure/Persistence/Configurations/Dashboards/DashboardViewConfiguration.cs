using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using Trap_Intel.Domain.Dashboards;
using Trap_Intel.Domain.Dashboards.Enums;
using Trap_Intel.Domain.Dashboards.ValueObjects;

namespace Trap_Intel.Infrastructure.Persistence.Configurations.Dashboards;

public class DashboardViewConfiguration : IEntityTypeConfiguration<DashboardView>
{
    public void Configure(EntityTypeBuilder<DashboardView> builder)
    {
        builder.ToTable("dashboard_views");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(d => d.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(d => d.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.Property(d => d.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(d => d.Description).HasColumnName("description").HasMaxLength(500);

        // Enums
        builder.Property(d => d.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(d => d.DefaultTimeRange)
            .HasColumnName("default_time_range")
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(DashboardTimeRange.Last24Hours);

        builder.Property(d => d.Theme)
            .HasColumnName("theme")
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(DashboardTheme.System);

        // Simple properties
        builder.Property(d => d.IsDefault).HasColumnName("is_default").HasDefaultValue(false);
        builder.Property(d => d.IsShared).HasColumnName("is_shared").HasDefaultValue(false);
        builder.Property(d => d.AutoRefreshSeconds).HasColumnName("auto_refresh_seconds").HasDefaultValue(0);
        builder.Property(d => d.ViewCount).HasColumnName("view_count").HasDefaultValue(0);

        // Timestamps
        builder.Property(d => d.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(d => d.LastViewedAt).HasColumnName("last_viewed_at");

        // Layout as owned entity
        builder.OwnsOne(d => d.Layout, layout =>
        {
            layout.Property(l => l.Type)
                .HasColumnName("layout_type")
                .HasConversion<string>()
                .HasMaxLength(50)
                .HasDefaultValue(LayoutType.Grid);

            layout.Property(l => l.Columns).HasColumnName("layout_columns").HasDefaultValue(4);
            layout.Property(l => l.RowHeight).HasColumnName("layout_row_height").HasDefaultValue(100);
            layout.Property(l => l.Gap).HasColumnName("layout_gap").HasDefaultValue(16);
            layout.Property(l => l.Padding).HasColumnName("layout_padding").HasDefaultValue(24);
            layout.Property(l => l.IsDraggable).HasColumnName("layout_is_draggable").HasDefaultValue(true);
            layout.Property(l => l.IsResizable).HasColumnName("layout_is_resizable").HasDefaultValue(true);
        });

        // Widgets as JSONB
        builder.Property(d => d.Widgets)
            .HasColumnName("widgets")
            .HasColumnType("jsonb")
            .HasConversion(
                v => SerializeWidgets(v),
                v => DeserializeWidgets(v),
                new ValueComparer<IReadOnlyList<DashboardWidget>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, w) => HashCode.Combine(a, w.GetHashCode())),
                    c => c.ToList()));

        // SharedWithUserIds as JSONB
        builder.Property(d => d.SharedWithUserIds)
            .HasColumnName("shared_with_user_ids")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>(),
                new ValueComparer<IReadOnlyList<Guid>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, g) => HashCode.Combine(a, g.GetHashCode())),
                    c => c.ToList()));

        // Indexes
        builder.HasIndex(d => d.UserId).HasDatabaseName("ix_dashboard_views_user_id");
        builder.HasIndex(d => d.OrganizationId).HasDatabaseName("ix_dashboard_views_organization_id");
        builder.HasIndex(d => d.Type).HasDatabaseName("ix_dashboard_views_type");
        builder.HasIndex(d => new { d.UserId, d.IsDefault }).HasDatabaseName("ix_dashboard_views_user_default");
        builder.HasIndex(d => new { d.OrganizationId, d.IsShared }).HasDatabaseName("ix_dashboard_views_org_shared");
    }

    private static string SerializeWidgets(IReadOnlyList<DashboardWidget> widgets)
    {
        if (widgets == null || widgets.Count == 0)
            return "[]";

        var list = widgets.Select(w => new
        {
            w.Id,
            Type = w.Type.ToString(),
            w.Title,
            Size = w.Size.ToString(),
            w.Row,
            w.Column,
            w.Configuration,
            DataSource = w.DataSource != null ? new
            {
                w.DataSource.SourceType,
                w.DataSource.Filter,
                w.DataSource.MaxItems,
                w.DataSource.SortBy,
                w.DataSource.SortDescending,
                TimeRangeOverride = w.DataSource.TimeRangeOverride?.ToString()
            } : null,
            w.IsVisible,
            w.RefreshIntervalOverride
        });

        return JsonSerializer.Serialize(list);
    }

    private static IReadOnlyList<DashboardWidget> DeserializeWidgets(string json)
    {
        try
        {
            if (string.IsNullOrEmpty(json) || json == "[]")
                return new List<DashboardWidget>();

            var elements = JsonSerializer.Deserialize<List<JsonElement>>(json);
            if (elements == null) return new List<DashboardWidget>();

            var widgets = new List<DashboardWidget>();
            foreach (var e in elements)
            {
                var id = e.GetProperty("Id").GetGuid();
                var typeStr = e.GetProperty("Type").GetString() ?? "Custom";
                var type = Enum.TryParse<WidgetType>(typeStr, out var wt) ? wt : WidgetType.Custom;
                var title = e.GetProperty("Title").GetString() ?? "";
                var sizeStr = e.TryGetProperty("Size", out var sz) ? sz.GetString() ?? "Medium" : "Medium";
                var size = Enum.TryParse<WidgetSize>(sizeStr, out var ws) ? ws : WidgetSize.Medium;
                var row = e.TryGetProperty("Row", out var r) ? r.GetInt32() : 0;
                var column = e.TryGetProperty("Column", out var c) ? c.GetInt32() : 0;
                var config = e.TryGetProperty("Configuration", out var cfg) && cfg.ValueKind != JsonValueKind.Null
                    ? cfg.GetString() : null;

                WidgetDataSource? dataSource = null;
                if (e.TryGetProperty("DataSource", out var ds) && ds.ValueKind != JsonValueKind.Null)
                {
                    var sourceType = ds.GetProperty("SourceType").GetString() ?? "attacks";
                    var filter = ds.TryGetProperty("Filter", out var f) && f.ValueKind != JsonValueKind.Null ? f.GetString() : null;
                    var maxItems = ds.TryGetProperty("MaxItems", out var mi) && mi.ValueKind != JsonValueKind.Null ? mi.GetInt32() : (int?)null;
                    var sortBy = ds.TryGetProperty("SortBy", out var sb) && sb.ValueKind != JsonValueKind.Null ? sb.GetString() : null;
                    var sortDesc = ds.TryGetProperty("SortDescending", out var sd) && sd.GetBoolean();

                    dataSource = new WidgetDataSource(sourceType, filter, maxItems, sortBy, sortDesc);
                }

                var isVisible = !e.TryGetProperty("IsVisible", out var iv) || iv.GetBoolean();
                var refreshOverride = e.TryGetProperty("RefreshIntervalOverride", out var rio) && rio.ValueKind != JsonValueKind.Null
                    ? rio.GetInt32() : (int?)null;

                var widget = new DashboardWidget(id, type, title, size, row, column, config, dataSource)
                {
                    IsVisible = isVisible,
                    RefreshIntervalOverride = refreshOverride
                };

                widgets.Add(widget);
            }

            return widgets;
        }
        catch { return new List<DashboardWidget>(); }
    }
}
