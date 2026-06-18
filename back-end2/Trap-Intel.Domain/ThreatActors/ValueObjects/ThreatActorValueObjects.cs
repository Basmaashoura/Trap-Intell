using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.ThreatActors.Enums;

namespace Trap_Intel.Domain.ThreatActors.ValueObjects;

/// <summary>
/// IP address associated with threat actor.
/// </summary>
public record ThreatActorIP
{
    public string IPAddress { get; init; } = string.Empty;
    public DateTime FirstSeenAt { get; init; }
    public DateTime LastSeenAt { get; init; }
    public int AttackCount { get; init; }
    public string? Country { get; init; }
    public string? ISP { get; init; }
    public string? ASN { get; init; }
    public bool IsBlocked { get; init; }
    public DateTime? BlockedAt { get; init; }

    public static ThreatActorIP Create(
        string ipAddress,
        string? country = null,
        string? isp = null,
        string? asn = null)
    {
        return new ThreatActorIP
        {
            IPAddress = ipAddress,
            FirstSeenAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow,
            AttackCount = 1,
            Country = country,
            ISP = isp,
            ASN = asn,
            IsBlocked = false
        };
    }

    public ThreatActorIP IncrementAttackCount()
    {
        return this with
        {
            AttackCount = AttackCount + 1,
            LastSeenAt = DateTime.UtcNow
        };
    }

    public ThreatActorIP MarkAsBlocked()
    {
        return this with
        {
            IsBlocked = true,
            BlockedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Behavior pattern observed from threat actor.
/// </summary>
public record BehaviorPattern
{
    public string PatternId { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public int Occurrences { get; init; }
    public DateTime FirstObservedAt { get; init; }
    public DateTime LastObservedAt { get; init; }

    public static BehaviorPattern Create(string category, string description)
    {
        return new BehaviorPattern
        {
            PatternId = Guid.NewGuid().ToString("N")[..8],
            Description = description,
            Category = category,
            Occurrences = 1,
            FirstObservedAt = DateTime.UtcNow,
            LastObservedAt = DateTime.UtcNow
        };
    }

    public BehaviorPattern IncrementOccurrence()
    {
        return this with
        {
            Occurrences = Occurrences + 1,
            LastObservedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Tactics, Techniques, and Procedures used by threat actor.
/// Based on MITRE ATT&CK framework.
/// </summary>
public record ThreatActorTTP
{
    public string TechniqueId { get; init; } = string.Empty;     // e.g., T1110
    public string TechniqueName { get; init; } = string.Empty;   // e.g., Brute Force
    public string TacticName { get; init; } = string.Empty;      // e.g., Credential Access
    public int UsageCount { get; init; }
    public DateTime FirstUsedAt { get; init; }
    public DateTime LastUsedAt { get; init; }

    public static ThreatActorTTP Create(string techniqueId, string techniqueName, string tacticName)
    {
        return new ThreatActorTTP
        {
            TechniqueId = techniqueId,
            TechniqueName = techniqueName,
            TacticName = tacticName,
            UsageCount = 1,
            FirstUsedAt = DateTime.UtcNow,
            LastUsedAt = DateTime.UtcNow
        };
    }

    public ThreatActorTTP IncrementUsage()
    {
        return this with
        {
            UsageCount = UsageCount + 1,
            LastUsedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Statistics about threat actor activity.
/// </summary>
public record ThreatActorStats
{
    public int TotalAttacks { get; init; }
    public int UniqueIPs { get; init; }
    public int HoneypotsTargeted { get; init; }
    public int CredentialsAttempted { get; init; }
    public int MalwareUploads { get; init; }
    public DateTime FirstAttackAt { get; init; }
    public DateTime LastAttackAt { get; init; }
    public TimeSpan AverageAttackInterval { get; init; }

    public static ThreatActorStats Initial(DateTime firstAttackAt)
    {
        return new ThreatActorStats
        {
            TotalAttacks = 1,
            UniqueIPs = 1,
            HoneypotsTargeted = 1,
            CredentialsAttempted = 0,
            MalwareUploads = 0,
            FirstAttackAt = firstAttackAt,
            LastAttackAt = firstAttackAt,
            AverageAttackInterval = TimeSpan.Zero
        };
    }

    public ThreatActorStats RecordAttack(
        bool newIP = false,
        bool newHoneypot = false,
        bool hasCredentials = false,
        bool hasMalware = false)
    {
        var now = DateTime.UtcNow;
        var newTotalAttacks = TotalAttacks + 1;
        var timeSinceFirst = now - FirstAttackAt;
        var avgInterval = newTotalAttacks > 1
            ? TimeSpan.FromTicks(timeSinceFirst.Ticks / (newTotalAttacks - 1))
            : TimeSpan.Zero;

        return this with
        {
            TotalAttacks = newTotalAttacks,
            UniqueIPs = newIP ? UniqueIPs + 1 : UniqueIPs,
            HoneypotsTargeted = newHoneypot ? HoneypotsTargeted + 1 : HoneypotsTargeted,
            CredentialsAttempted = hasCredentials ? CredentialsAttempted + 1 : CredentialsAttempted,
            MalwareUploads = hasMalware ? MalwareUploads + 1 : MalwareUploads,
            LastAttackAt = now,
            AverageAttackInterval = avgInterval
        };
    }
}

/// <summary>
/// Intelligence note about threat actor.
/// </summary>
public record ThreatIntelNote
{
    public string NoteId { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public Guid CreatedByUserId { get; init; }
    public DateTime CreatedAt { get; init; }

    public static ThreatIntelNote Create(string content, string source, Guid userId)
    {
        return new ThreatIntelNote
        {
            NoteId = Guid.NewGuid().ToString("N")[..8],
            Content = content,
            Source = source,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Threat score breakdown.
/// </summary>
public record ThreatScoreBreakdown
{
    public decimal BaseScore { get; init; }
    public decimal FrequencyModifier { get; init; }
    public decimal SeverityModifier { get; init; }
    public decimal TTPModifier { get; init; }
    public decimal RecencyModifier { get; init; }
    public decimal TotalScore { get; init; }

    public static ThreatScoreBreakdown Calculate(
        int totalAttacks,
        int highSeverityAttacks,
        int uniqueTTPs,
        DateTime lastAttackAt)
    {
        // Base score from attack volume (0-30)
        var baseScore = Math.Min(30, totalAttacks * 2);

        // Frequency modifier (0-20)
        var frequencyModifier = Math.Min(20, totalAttacks / 5 * 4);

        // Severity modifier (0-25)
        var severityModifier = Math.Min(25, highSeverityAttacks * 5);

        // TTP diversity modifier (0-15)
        var ttpModifier = Math.Min(15, uniqueTTPs * 3);

        // Recency modifier (0-10) - higher if recent
        var daysSinceLastAttack = (DateTime.UtcNow - lastAttackAt).TotalDays;
        var recencyModifier = daysSinceLastAttack switch
        {
            < 1 => 10,
            < 7 => 8,
            < 30 => 5,
            < 90 => 2,
            _ => 0
        };

        var total = Math.Min(100, baseScore + frequencyModifier + severityModifier + 
                                  ttpModifier + (decimal)recencyModifier);

        return new ThreatScoreBreakdown
        {
            BaseScore = baseScore,
            FrequencyModifier = frequencyModifier,
            SeverityModifier = severityModifier,
            TTPModifier = ttpModifier,
            RecencyModifier = (decimal)recencyModifier,
            TotalScore = total
        };
    }
}
