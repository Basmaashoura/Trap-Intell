using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Domain.ThreatActors.Entities;

/// <summary>
/// Represents an IP address associated with a threat actor.
/// Child entity owned by ThreatActor aggregate.
/// Tracks attack history, geolocation, and blocking status.
/// </summary>
public class ThreatActorIPEntity : Entity<Guid>
{
    // Private constructor for EF
    private ThreatActorIPEntity() { }

    private ThreatActorIPEntity(
        Guid id,
        Guid threatActorId,
        string ipAddress)
        : base(id)
    {
        ThreatActorId = threatActorId;
        IPAddress = ipAddress;
        FirstSeenAt = DateTime.UtcNow;
        LastSeenAt = DateTime.UtcNow;
        AttackCount = 1;
        IsBlocked = false;
        ReputationScore = 0;
    }

    #region Properties

    /// <summary>
    /// Parent threat actor ID.
    /// </summary>
    public Guid ThreatActorId { get; private set; }

    /// <summary>
    /// The IP address.
    /// </summary>
    public string IPAddress { get; private set; } = string.Empty;

    /// <summary>
    /// When this IP was first seen attacking.
    /// </summary>
    public DateTime FirstSeenAt { get; private set; }

    /// <summary>
    /// When this IP was most recently seen attacking.
    /// </summary>
    public DateTime LastSeenAt { get; private set; }

    /// <summary>
    /// Number of attacks from this IP.
    /// </summary>
    public int AttackCount { get; private set; }

    /// <summary>
    /// Country of origin.
    /// </summary>
    public string? Country { get; private set; }

    /// <summary>
    /// Country code (ISO 3166-1 alpha-2).
    /// </summary>
    public string? CountryCode { get; private set; }

    /// <summary>
    /// City of origin.
    /// </summary>
    public string? City { get; private set; }

    /// <summary>
    /// Region/State.
    /// </summary>
    public string? Region { get; private set; }

    /// <summary>
    /// Internet Service Provider.
    /// </summary>
    public string? ISP { get; private set; }

    /// <summary>
    /// Autonomous System Number.
    /// </summary>
    public string? ASN { get; private set; }

    /// <summary>
    /// Whether this IP is currently blocked.
    /// </summary>
    public bool IsBlocked { get; private set; }

    /// <summary>
    /// When the IP was blocked.
    /// </summary>
    public DateTime? BlockedAt { get; private set; }

    /// <summary>
    /// User who blocked the IP.
    /// </summary>
    public Guid? BlockedByUserId { get; private set; }

    /// <summary>
    /// Reason for blocking.
    /// </summary>
    public string? BlockReason { get; private set; }

    /// <summary>
    /// When the IP was unblocked (if applicable).
    /// </summary>
    public DateTime? UnblockedAt { get; private set; }

    /// <summary>
    /// User who unblocked the IP.
    /// </summary>
    public Guid? UnblockedByUserId { get; private set; }

    /// <summary>
    /// Reason for unblocking.
    /// </summary>
    public string? UnblockReason { get; private set; }

    /// <summary>
    /// Threat reputation score (0-100, higher = more dangerous).
    /// </summary>
    public int ReputationScore { get; private set; }

    /// <summary>
    /// Whether this is the primary IP (most attacks).
    /// </summary>
    public bool IsPrimary { get; private set; }

    /// <summary>
    /// IP type classification.
    /// </summary>
    public IPType IPType { get; private set; } = IPType.Unknown;

    /// <summary>
    /// Additional metadata as JSON.
    /// </summary>
    public string? Metadata { get; private set; }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Create a new IP entity.
    /// </summary>
    public static Result<ThreatActorIPEntity> Create(
        Guid threatActorId,
        string ipAddress,
        string? country = null,
        string? countryCode = null,
        string? city = null,
        string? isp = null,
        string? asn = null)
    {
        if (threatActorId == Guid.Empty)
            return Result.Failure<ThreatActorIPEntity>(ThreatActorErrors.InvalidThreatActorId);

        if (string.IsNullOrWhiteSpace(ipAddress))
            return Result.Failure<ThreatActorIPEntity>(ThreatActorErrors.InvalidIPAddress);

        var entity = new ThreatActorIPEntity(
            Guid.NewGuid(),
            threatActorId,
            ipAddress.Trim())
        {
            Country = country,
            CountryCode = countryCode,
            City = city,
            ISP = isp,
            ASN = asn
        };

        return Result.Success(entity);
    }

    /// <summary>
    /// Reconstruct from database.
    /// </summary>
    public static ThreatActorIPEntity Reconstruct(
        Guid id,
        Guid threatActorId,
        string ipAddress,
        DateTime firstSeenAt,
        DateTime lastSeenAt,
        int attackCount,
        string? country,
        string? countryCode,
        string? city,
        string? region,
        string? isp,
        string? asn,
        bool isBlocked,
        DateTime? blockedAt,
        Guid? blockedByUserId,
        string? blockReason,
        DateTime? unblockedAt,
        Guid? unblockedByUserId,
        string? unblockReason,
        int reputationScore,
        bool isPrimary,
        IPType ipType,
        string? metadata)
    {
        return new ThreatActorIPEntity
        {
            Id = id,
            ThreatActorId = threatActorId,
            IPAddress = ipAddress,
            FirstSeenAt = firstSeenAt,
            LastSeenAt = lastSeenAt,
            AttackCount = attackCount,
            Country = country,
            CountryCode = countryCode,
            City = city,
            Region = region,
            ISP = isp,
            ASN = asn,
            IsBlocked = isBlocked,
            BlockedAt = blockedAt,
            BlockedByUserId = blockedByUserId,
            BlockReason = blockReason,
            UnblockedAt = unblockedAt,
            UnblockedByUserId = unblockedByUserId,
            UnblockReason = unblockReason,
            ReputationScore = reputationScore,
            IsPrimary = isPrimary,
            IPType = ipType,
            Metadata = metadata
        };
    }

    #endregion

    #region Domain Behaviors

    /// <summary>
    /// Record an attack from this IP.
    /// </summary>
    public void RecordAttack()
    {
        AttackCount++;
        LastSeenAt = DateTime.UtcNow;
        UpdateReputationScore();
    }

    /// <summary>
    /// Block this IP address.
    /// </summary>
    public Result Block(Guid userId, string reason)
    {
        if (userId == Guid.Empty)
            return Result.Failure(ThreatActorErrors.InvalidUserId);

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(ThreatActorErrors.InvalidReason);

        if (IsBlocked)
            return Result.Failure(ThreatActorErrors.IPAlreadyBlocked);

        IsBlocked = true;
        BlockedAt = DateTime.UtcNow;
        BlockedByUserId = userId;
        BlockReason = reason.Trim();

        // Clear unblock info
        UnblockedAt = null;
        UnblockedByUserId = null;
        UnblockReason = null;

        return Result.Success();
    }

    /// <summary>
    /// Unblock this IP address.
    /// </summary>
    public Result Unblock(Guid userId, string reason)
    {
        if (userId == Guid.Empty)
            return Result.Failure(ThreatActorErrors.InvalidUserId);

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(ThreatActorErrors.InvalidReason);

        if (!IsBlocked)
            return Result.Failure(ThreatActorErrors.IPNotBlocked);

        IsBlocked = false;
        UnblockedAt = DateTime.UtcNow;
        UnblockedByUserId = userId;
        UnblockReason = reason.Trim();

        return Result.Success();
    }

    /// <summary>
    /// Update geolocation information.
    /// </summary>
    public Result UpdateGeolocation(
        string? country,
        string? countryCode,
        string? city,
        string? region,
        string? isp,
        string? asn)
    {
        Country = country;
        CountryCode = countryCode;
        City = city;
        Region = region;
        ISP = isp;
        ASN = asn;

        return Result.Success();
    }

    /// <summary>
    /// Set IP type classification.
    /// </summary>
    public void SetIPType(IPType type)
    {
        IPType = type;
    }

    /// <summary>
    /// Mark as primary IP.
    /// </summary>
    public void MarkAsPrimary()
    {
        IsPrimary = true;
    }

    /// <summary>
    /// Unmark as primary IP.
    /// </summary>
    public void UnmarkAsPrimary()
    {
        IsPrimary = false;
    }

    /// <summary>
    /// Set metadata.
    /// </summary>
    public void SetMetadata(string metadata)
    {
        Metadata = metadata;
    }

    /// <summary>
    /// Manually set reputation score.
    /// </summary>
    public Result SetReputationScore(int score)
    {
        if (score < 0 || score > 100)
            return Result.Failure(ThreatActorErrors.InvalidReputationScore);

        ReputationScore = score;
        return Result.Success();
    }

    #endregion

    #region Query Methods

    /// <summary>
    /// Get days since first seen.
    /// </summary>
    public int GetDaysSinceFirstSeen() => (DateTime.UtcNow - FirstSeenAt).Days;

    /// <summary>
    /// Get days since last seen.
    /// </summary>
    public int GetDaysSinceLastSeen() => (DateTime.UtcNow - LastSeenAt).Days;

    /// <summary>
    /// Check if IP is recently active (within specified days).
    /// </summary>
    public bool IsRecentlyActive(int days = 7) => GetDaysSinceLastSeen() <= days;

    /// <summary>
    /// Get duration blocked (if blocked).
    /// </summary>
    public TimeSpan? GetBlockedDuration() => IsBlocked && BlockedAt.HasValue
        ? DateTime.UtcNow - BlockedAt.Value
        : null;

    /// <summary>
    /// Check if IP is high risk based on reputation.
    /// </summary>
    public bool IsHighRisk() => ReputationScore >= 70;

    /// <summary>
    /// Get formatted location string.
    /// </summary>
    public string GetLocationString()
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(City)) parts.Add(City);
        if (!string.IsNullOrEmpty(Region)) parts.Add(Region);
        if (!string.IsNullOrEmpty(Country)) parts.Add(Country);
        return parts.Count > 0 ? string.Join(", ", parts) : "Unknown";
    }

    #endregion

    #region Private Methods

    private void UpdateReputationScore()
    {
        // Simple reputation calculation based on attack frequency
        var daysSinceFirst = Math.Max(1, GetDaysSinceFirstSeen());
        var attacksPerDay = (decimal)AttackCount / daysSinceFirst;

        ReputationScore = attacksPerDay switch
        {
            >= 10 => 100,
            >= 5 => 90,
            >= 2 => 70,
            >= 1 => 50,
            >= 0.5m => 30,
            _ => Math.Min(100, AttackCount * 5)
        };
    }

    #endregion
}

/// <summary>
/// Classification of IP address type.
/// </summary>
public enum IPType
{
    Unknown = 0,
    Residential = 1,
    Commercial = 2,
    DataCenter = 3,
    CloudProvider = 4,
    VPN = 5,
    Proxy = 6,
    TorExitNode = 7,
    Mobile = 8,
    Satellite = 9,
    Hosting = 10
}
