using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using Trap_Intel.Domain.ThreatActors;
using Trap_Intel.Domain.ThreatActors.Entities;
using Trap_Intel.Domain.ThreatActors.Enums;
using Trap_Intel.Domain.ThreatActors.ValueObjects;

namespace Trap_Intel.Infrastructure.Persistence.Configurations.ThreatActors;

public class ThreatActorConfiguration : IEntityTypeConfiguration<ThreatActor>
{
    public void Configure(EntityTypeBuilder<ThreatActor> builder)
    {
        builder.ToTable("threat_actors");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(t => t.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.Property(t => t.Alias).HasColumnName("alias").HasMaxLength(100);

        builder.Property(t => t.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(t => t.ThreatLevel).HasColumnName("threat_level").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(t => t.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(t => t.Confidence).HasColumnName("confidence").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(t => t.Motivation).HasColumnName("motivation").HasConversion<string>().HasMaxLength(50).HasDefaultValue(ThreatMotivation.Unknown);
        builder.Property(t => t.Region).HasColumnName("region").HasConversion<string>().HasMaxLength(50).HasDefaultValue(ThreatRegion.Unknown);
        builder.Property(t => t.ThreatScore).HasColumnName("threat_score").HasPrecision(5, 2).IsRequired();

        builder.Property(t => t.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // Stats (owned)
        builder.OwnsOne(t => t.Stats, stats =>
        {
            stats.Property(s => s.TotalAttacks).HasColumnName("stats_total_attacks").HasDefaultValue(0);
            stats.Property(s => s.UniqueIPs).HasColumnName("stats_unique_ips").HasDefaultValue(0);
            stats.Property(s => s.HoneypotsTargeted).HasColumnName("stats_unique_honeypots").HasDefaultValue(0);
            stats.Property(s => s.CredentialsAttempted).HasColumnName("stats_credentials").HasDefaultValue(0);
            stats.Property(s => s.MalwareUploads).HasColumnName("stats_malware").HasDefaultValue(0);
            stats.Property(s => s.FirstAttackAt).HasColumnName("stats_first_attack_at");
            stats.Property(s => s.LastAttackAt).HasColumnName("stats_last_attack_at");
            stats.Ignore(s => s.AverageAttackInterval);
        });

        // ScoreBreakdown (owned, nullable)
        builder.OwnsOne(t => t.ScoreBreakdown, score =>
        {
            score.Property(s => s.BaseScore).HasColumnName("score_base").HasPrecision(5, 2);
            score.Property(s => s.FrequencyModifier).HasColumnName("score_frequency").HasPrecision(5, 2);
            score.Property(s => s.SeverityModifier).HasColumnName("score_severity").HasPrecision(5, 2);
            score.Property(s => s.TTPModifier).HasColumnName("score_ttp").HasPrecision(5, 2);
            score.Property(s => s.RecencyModifier).HasColumnName("score_recency").HasPrecision(5, 2);
            score.Ignore(s => s.TotalScore);
        });

        // CorrelatedAttackIds as JSONB
        builder.Property(t => t.CorrelatedAttackIds)
            .HasColumnName("correlated_attack_ids")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>(),
                new ValueComparer<IReadOnlyList<Guid>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, g) => HashCode.Combine(a, g.GetHashCode())),
                    c => c.ToList()));

        // TargetedHoneypotIds as JSONB
        builder.Property(t => t.TargetedHoneypotIds)
            .HasColumnName("targeted_honeypot_ids")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>(),
                new ValueComparer<IReadOnlyList<Guid>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, g) => HashCode.Combine(a, g.GetHashCode())),
                    c => c.ToList()));

        // Child entities
        builder.HasMany(t => t.AssociatedIPs)
            .WithOne()
            .HasForeignKey(ip => ip.ThreatActorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.ObservedTTPs)
            .WithOne()
            .HasForeignKey(ttp => ttp.ThreatActorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.BehaviorPatterns)
            .WithOne()
            .HasForeignKey(bp => bp.ThreatActorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.IntelNotes)
            .WithOne()
            .HasForeignKey(n => n.ThreatActorId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(t => t.OrganizationId).HasDatabaseName("ix_threat_actors_organization_id");
        builder.HasIndex(t => t.ThreatLevel).HasDatabaseName("ix_threat_actors_threat_level");
        builder.HasIndex(t => t.Status).HasDatabaseName("ix_threat_actors_status");
        builder.HasIndex(t => t.ThreatScore).HasDatabaseName("ix_threat_actors_threat_score");
        builder.HasIndex(t => new { t.OrganizationId, t.ThreatLevel }).HasDatabaseName("ix_threat_actors_org_level");
    }
}

public class ThreatActorIPEntityConfiguration : IEntityTypeConfiguration<ThreatActorIPEntity>
{
    public void Configure(EntityTypeBuilder<ThreatActorIPEntity> builder)
    {
        builder.ToTable("threat_actor_ips");

        builder.HasKey(ip => ip.Id);
        builder.Property(ip => ip.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(ip => ip.ThreatActorId).HasColumnName("threat_actor_id").IsRequired();

        builder.Property(ip => ip.IPAddress).HasColumnName("ip_address").HasMaxLength(45).IsRequired();
        builder.Property(ip => ip.IPType).HasColumnName("ip_type").HasConversion<string>().HasMaxLength(20);
        builder.Property(ip => ip.Country).HasColumnName("country").HasMaxLength(100);
        builder.Property(ip => ip.CountryCode).HasColumnName("country_code").HasMaxLength(2);
        builder.Property(ip => ip.City).HasColumnName("city").HasMaxLength(100);
        builder.Property(ip => ip.Region).HasColumnName("region").HasMaxLength(100);
        builder.Property(ip => ip.ISP).HasColumnName("isp").HasMaxLength(255);
        builder.Property(ip => ip.ASN).HasColumnName("asn").HasMaxLength(50);

        builder.Property(ip => ip.AttackCount).HasColumnName("attack_count").HasDefaultValue(0);
        builder.Property(ip => ip.FirstSeenAt).HasColumnName("first_seen_at").IsRequired();
        builder.Property(ip => ip.LastSeenAt).HasColumnName("last_seen_at").IsRequired();
        builder.Property(ip => ip.IsPrimary).HasColumnName("is_primary").HasDefaultValue(false);
        builder.Property(ip => ip.IsBlocked).HasColumnName("is_blocked").HasDefaultValue(false);
        builder.Property(ip => ip.BlockedAt).HasColumnName("blocked_at");
        builder.Property(ip => ip.BlockedByUserId).HasColumnName("blocked_by_user_id");
        builder.Property(ip => ip.BlockReason).HasColumnName("block_reason").HasMaxLength(500);

        builder.HasIndex(ip => ip.ThreatActorId).HasDatabaseName("ix_threat_actor_ips_threat_actor_id");
        builder.HasIndex(ip => ip.IPAddress).HasDatabaseName("ix_threat_actor_ips_ip_address");
    }
}

public class ThreatActorTTPEntityConfiguration : IEntityTypeConfiguration<ThreatActorTTPEntity>
{
    public void Configure(EntityTypeBuilder<ThreatActorTTPEntity> builder)
    {
        builder.ToTable("threat_actor_ttps");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(t => t.ThreatActorId).HasColumnName("threat_actor_id").IsRequired();

        builder.Property(t => t.TechniqueId).HasColumnName("technique_id").HasMaxLength(20).IsRequired();
        builder.Property(t => t.TechniqueName).HasColumnName("technique_name").HasMaxLength(255).IsRequired();
        builder.Property(t => t.TacticId).HasColumnName("tactic_id").HasMaxLength(20);
        builder.Property(t => t.TacticName).HasColumnName("tactic_name").HasMaxLength(255).IsRequired();
        builder.Property(t => t.SubTechniqueId).HasColumnName("sub_technique_id").HasMaxLength(20);
        builder.Property(t => t.SubTechniqueName).HasColumnName("sub_technique_name").HasMaxLength(255);

        builder.Property(t => t.UsageCount).HasColumnName("usage_count").HasDefaultValue(0);
        builder.Property(t => t.FirstUsedAt).HasColumnName("first_used_at").IsRequired();
        builder.Property(t => t.LastUsedAt).HasColumnName("last_used_at").IsRequired();
        builder.Property(t => t.IsSignatureTTP).HasColumnName("is_signature").HasDefaultValue(false);

        // ObservedInAttackIds as JSONB
        builder.Property(t => t.ObservedInAttackIds)
            .HasColumnName("observed_in_attack_ids")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>(),
                new ValueComparer<IReadOnlyList<Guid>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, g) => HashCode.Combine(a, g.GetHashCode())),
                    c => c.ToList()));

        builder.HasIndex(t => t.ThreatActorId).HasDatabaseName("ix_threat_actor_ttps_threat_actor_id");
        builder.HasIndex(t => t.TechniqueId).HasDatabaseName("ix_threat_actor_ttps_technique_id");
    }
}

public class BehaviorPatternEntityConfiguration : IEntityTypeConfiguration<BehaviorPatternEntity>
{
    public void Configure(EntityTypeBuilder<BehaviorPatternEntity> builder)
    {
        builder.ToTable("behavior_patterns");

        builder.HasKey(bp => bp.Id);
        builder.Property(bp => bp.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(bp => bp.ThreatActorId).HasColumnName("threat_actor_id").IsRequired();

        builder.Property(bp => bp.PatternType).HasColumnName("pattern_type").HasConversion<string>().HasMaxLength(50);
        builder.Property(bp => bp.Category).HasColumnName("category").HasMaxLength(100).IsRequired();
        builder.Property(bp => bp.Description).HasColumnName("description").HasMaxLength(2000).IsRequired();
        builder.Property(bp => bp.Occurrences).HasColumnName("occurrences").HasDefaultValue(0);
        builder.Property(bp => bp.FirstObservedAt).HasColumnName("first_observed_at").IsRequired();
        builder.Property(bp => bp.LastObservedAt).HasColumnName("last_observed_at").IsRequired();
        builder.Property(bp => bp.IsDistinctive).HasColumnName("is_distinctive").HasDefaultValue(false);
        builder.Property(bp => bp.DetectedByAI).HasColumnName("detected_by_ai").HasDefaultValue(false);

        // Enum - Severity
        builder.Property(bp => bp.Severity)
            .HasColumnName("severity")
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(PatternSeverity.Medium);

        // Additional properties
        builder.Property(bp => bp.ConfidenceScore).HasColumnName("confidence_score").HasDefaultValue(50);
        builder.Property(bp => bp.IdentifiedByUserId).HasColumnName("identified_by_user_id");
        builder.Property(bp => bp.Notes).HasColumnName("notes").HasMaxLength(5000);
        builder.Property(bp => bp.Indicators).HasColumnName("indicators").HasColumnType("jsonb");
        builder.Property(bp => bp.Metadata).HasColumnName("metadata").HasColumnType("jsonb");

        // ObservedInAttackIds as JSONB
        builder.Property(bp => bp.ObservedInAttackIds)
            .HasColumnName("observed_in_attack_ids")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>(),
                new ValueComparer<IReadOnlyList<Guid>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, g) => HashCode.Combine(a, g.GetHashCode())),
                    c => c.ToList()));

        builder.HasIndex(bp => bp.ThreatActorId).HasDatabaseName("ix_behavior_patterns_threat_actor_id");
        builder.HasIndex(bp => bp.Severity).HasDatabaseName("ix_behavior_patterns_severity");
        builder.HasIndex(bp => bp.PatternType).HasDatabaseName("ix_behavior_patterns_pattern_type");
    }
}

public class ThreatIntelNoteEntityConfiguration : IEntityTypeConfiguration<ThreatIntelNoteEntity>
{
    public void Configure(EntityTypeBuilder<ThreatIntelNoteEntity> builder)
    {
        builder.ToTable("threat_intel_notes");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(n => n.ThreatActorId).HasColumnName("threat_actor_id").IsRequired();

        builder.Property(n => n.NoteType).HasColumnName("note_type").HasConversion<string>().HasMaxLength(50);
        builder.Property(n => n.Content).HasColumnName("content").HasMaxLength(10000).IsRequired();
        builder.Property(n => n.Source).HasColumnName("source").HasMaxLength(255).IsRequired();
        builder.Property(n => n.AuthorUserId).HasColumnName("author_user_id").IsRequired();
        builder.Property(n => n.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(n => n.EditedAt).HasColumnName("edited_at");
        builder.Property(n => n.EditedByUserId).HasColumnName("edited_by_user_id");
        builder.Property(n => n.IsEdited).HasColumnName("is_edited").HasDefaultValue(false);
        builder.Property(n => n.IsInternal).HasColumnName("is_internal").HasDefaultValue(true);
        builder.Property(n => n.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(n => n.DeletedAt).HasColumnName("deleted_at");
        builder.Property(n => n.DeletedByUserId).HasColumnName("deleted_by_user_id");
        builder.Property(n => n.IsPinned).HasColumnName("is_pinned").HasDefaultValue(false);

        // Enum - ConfidenceLevel
        builder.Property(n => n.ConfidenceLevel)
            .HasColumnName("confidence_level")
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(IntelConfidenceLevel.Medium);

        // JSONB - RelatedAttackIds
        builder.Property(n => n.RelatedAttackIds)
            .HasColumnName("related_attack_ids")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>());

        // JSONB - Tags
        builder.Property(n => n.Tags)
            .HasColumnName("tags")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

        // Additional properties
        builder.Property(n => n.ExternalUrl).HasColumnName("external_url").HasMaxLength(2000);
        builder.Property(n => n.Metadata).HasColumnName("metadata").HasColumnType("jsonb");

        builder.HasIndex(n => n.ThreatActorId).HasDatabaseName("ix_threat_intel_notes_threat_actor_id");
        builder.HasIndex(n => n.ConfidenceLevel).HasDatabaseName("ix_threat_intel_notes_confidence");
    }
}
