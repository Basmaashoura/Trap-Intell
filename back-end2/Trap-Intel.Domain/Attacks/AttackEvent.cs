using System.Text.Json;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Attacks.Enums;
using Trap_Intel.Domain.Attacks.Events;
using Trap_Intel.Domain.Attacks.ValueObjects;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Domain.Attacks;

/// <summary>
/// Attack event captured by Go honeypot and received via gRPC.
/// Represents a single interaction attempt by an attacker.
/// </summary>
public class AttackEvent : AggregateRoot<Guid>
{
    // Private constructor for EF
    private AttackEvent() { }

    private AttackEvent(
        Guid id,
        Guid honeypotId,
        Guid organizationId,
        string externalEventId)
        : base(id)
    {
        HoneypotId = honeypotId;
        OrganizationId = organizationId;
        ExternalEventId = externalEventId;
        ReceivedAt = DateTime.UtcNow;
        IsAnalyzed = false;
    }

    #region Properties - Metadata (Relational, Indexed)

    /// <summary>
    /// Which honeypot captured this attack
    /// </summary>
    public Guid HoneypotId { get; private set; }

    /// <summary>
    /// Organization that owns the honeypot
    /// </summary>
    public Guid OrganizationId { get; private set; }

    /// <summary>
    /// Event ID from Go honeypot (for deduplication)
    /// </summary>
    public string ExternalEventId { get; private set; } = string.Empty;

    /// <summary>
    /// When attack was captured (Go honeypot time)
    /// </summary>
    public DateTime Timestamp { get; private set; }

    /// <summary>
    /// Source endpoint (attacker)
    /// </summary>
    public NetworkEndpoint SourceEndpoint { get; private set; } = null!;

    /// <summary>
    /// Target endpoint (honeypot)
    /// </summary>
    public NetworkEndpoint TargetEndpoint { get; private set; } = null!;

    /// <summary>
    /// Sensor/agent ID from Go honeypot
    /// </summary>
    public string SensorId { get; private set; } = string.Empty;

    /// <summary>
    /// Type of attack
    /// </summary>
    public AttackType AttackType { get; private set; }

    /// <summary>
    /// Protocol used
    /// </summary>
    public AttackProtocol Protocol { get; private set; }

    /// <summary>
    /// Severity (can be updated by AI)
    /// </summary>
    public AttackSeverity Severity { get; private set; }

    #endregion

    #region Properties - AI Analysis (Updated Asynchronously)

    /// <summary>
    /// Whether AI has analyzed this event
    /// </summary>
    public bool IsAnalyzed { get; private set; }

    /// <summary>
    /// Threat score (0-100, AI-calculated)
    /// </summary>
    public decimal ThreatScore { get; private set; }

    /// <summary>
    /// Attack intent (AI-classified)
    /// </summary>
    public AttackIntent Intent { get; private set; }

    /// <summary>
    /// MITRE ATT&CK techniques (AI-extracted)
    /// </summary>
    private List<MitreTechnique> _mitreTechniques = new();
    public IReadOnlyList<MitreTechnique> MitreTechniques => _mitreTechniques.AsReadOnly();

    /// <summary>
    /// Whether AI detected anomaly
    /// </summary>
    public bool IsAnomaly { get; private set; }

    #endregion

    #region Properties - Captured Data

    /// <summary>
    /// Credentials attempted (if login attack)
    /// </summary>
    public AttackCredentials? Credentials { get; private set; }

    /// <summary>
    /// Command executed (if command injection)
    /// </summary>
    public string? Command { get; private set; }

    /// <summary>
    /// Malware payload (if malware upload)
    /// </summary>
    public byte[]? Payload { get; private set; }

    /// <summary>
    /// File hash (SHA256, if file uploaded)
    /// </summary>
    public string? FileHash { get; private set; }

    /// <summary>
    /// User agent string
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// HTTP headers (if HTTP attack)
    /// </summary>
    private Dictionary<string, string> _headers = new();
    public IReadOnlyDictionary<string, string> Headers => _headers;

    #endregion

    #region Properties - Context

    /// <summary>
    /// Geolocation of attacker
    /// </summary>
    public GeoLocation Geolocation { get; private set; } = null!;

    /// <summary>
    /// Session ID from Go honeypot
    /// </summary>
    public long SessionId { get; private set; }

    /// <summary>
    /// When .NET platform received this event
    /// </summary>
    public DateTime ReceivedAt { get; private set; }

    /// <summary>
    /// Whether event was pre-filtered by Go agent
    /// </summary>
    public bool WasEdgeFiltered { get; private set; }

    /// <summary>
    /// Reason for edge filtering (if applicable)
    /// </summary>
    public string? FilterReason { get; private set; }

    #endregion

    #region Properties - Raw Data (JSONB, Flexible)

    /// <summary>
    /// Full raw JSON payload from Go honeypot
    /// Stored in PostgreSQL JSONB column for flexible querying
    /// </summary>
    public string RawDataJson { get; private set; } = string.Empty;

    /// <summary>
    /// Check if raw data is present
    /// </summary>
    public bool HasRawData => !string.IsNullOrWhiteSpace(RawDataJson);

    #endregion

    #region Properties - Relationships

    /// <summary>
    /// Linked threat actor (set after correlation)
    /// </summary>
    public Guid? ThreatActorId { get; private set; }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Create from external attack event data (gRPC, HTTP, etc.)
    /// Uses IAttackEventData interface to decouple from transport mechanism.
    /// </summary>
    public static Result<AttackEvent> Create(
        Guid honeypotId,
        Guid organizationId,
        IAttackEventData data)
    {
        // Validation
        if (data == null)
            return Result.Failure<AttackEvent>(AttackErrors.InvalidData);

        if (string.IsNullOrWhiteSpace(data.ExternalEventId))
            return Result.Failure<AttackEvent>(
                Error.Custom("AttackEvent.MissingExternalId", "External event ID is required"));

        if (string.IsNullOrWhiteSpace(data.SourceIP))
            return Result.Failure<AttackEvent>(
                Error.Custom("AttackEvent.MissingSourceIP", "Source IP is required"));

        // Create endpoints
        var sourceEndpointResult = NetworkEndpoint.Create(data.SourceIP, data.SourcePort);
        if (sourceEndpointResult.IsFailure)
            return Result.Failure<AttackEvent>(sourceEndpointResult.Errors[0]);

        var targetEndpointResult = NetworkEndpoint.Create("0.0.0.0", data.TargetPort);
        if (targetEndpointResult.IsFailure)
            return Result.Failure<AttackEvent>(targetEndpointResult.Errors[0]);

        // Create geolocation
        var geolocation = data.Geolocation != null
            ? GeoLocation.Create(
                data.Geolocation.Country ?? "Unknown",
                data.Geolocation.CountryCode ?? "XX",
                data.Geolocation.City ?? "Unknown",
                data.Geolocation.Latitude,
                data.Geolocation.Longitude,
                data.Geolocation.Region,
                data.Geolocation.ISP,
                data.Geolocation.ASN).Value
            : GeoLocation.Unknown();

        // Create attack event
        var attackEvent = new AttackEvent(
            Guid.NewGuid(),
            honeypotId,
            organizationId,
            data.ExternalEventId)
        {
            Timestamp = data.Timestamp,
            SourceEndpoint = sourceEndpointResult.Value,
            TargetEndpoint = targetEndpointResult.Value,
            SensorId = data.SensorId ?? string.Empty,
            AttackType = MapAttackType(data.EventType),
            Protocol = MapProtocol(data.Protocol),
            Severity = MapSeverity(data.Severity),
            Geolocation = geolocation,
            SessionId = data.SessionId,
            WasEdgeFiltered = data.WasEdgeFiltered,
            FilterReason = data.FilterReason,
            RawDataJson = data.RawPayloadJson ?? "{}"
        };

        // Set optional fields
        if (!string.IsNullOrWhiteSpace(data.Username) || !string.IsNullOrWhiteSpace(data.Password))
        {
            attackEvent.Credentials = AttackCredentials.Create(data.Username, data.Password);
        }

        if (!string.IsNullOrWhiteSpace(data.Command))
        {
            attackEvent.Command = data.Command;
        }

        if (data.Payload != null && data.Payload.Length > 0)
        {
            attackEvent.Payload = data.Payload;
            attackEvent.FileHash = ComputeFileHash(data.Payload);
        }

        if (!string.IsNullOrWhiteSpace(data.UserAgent))
        {
            attackEvent.UserAgent = data.UserAgent;
        }

        if (data.Headers != null && data.Headers.Count > 0)
        {
            attackEvent._headers = new Dictionary<string, string>(data.Headers);
        }

        // Raise domain event
        attackEvent.RaiseDomainEvent(new AttackEventReceivedEvent(
            attackEvent.Id,
            honeypotId,
            organizationId,
            data.EventType,
            data.Severity,
            data.SourceIP,
            DateTime.UtcNow));

        // If high severity, raise immediate alert event
        if (attackEvent.Severity == AttackSeverity.High || attackEvent.Severity == AttackSeverity.Critical)
        {
            attackEvent.RaiseDomainEvent(new HighSeverityAttackDetectedEvent(
                attackEvent.Id,
                honeypotId,
                organizationId,
                data.SourceIP,
                attackEvent.Severity,
                0,  // Threat score not yet calculated
                DateTime.UtcNow));
        }

        // If malware upload, raise specific event
        if (attackEvent.Payload != null && attackEvent.FileHash != null)
        {
            attackEvent.RaiseDomainEvent(new MalwareUploadedEvent(
                attackEvent.Id,
                honeypotId,
                organizationId,
                data.SourceIP,
                attackEvent.FileHash,
                attackEvent.Payload.Length,
                DateTime.UtcNow));
        }

        return Result.Success(attackEvent);
    }

    /// <summary>
    /// Reconstruct from database
    /// </summary>
    public static AttackEvent Reconstruct(
        Guid id,
        Guid honeypotId,
        Guid organizationId,
        string externalEventId,
        DateTime timestamp,
        NetworkEndpoint sourceEndpoint,
        NetworkEndpoint targetEndpoint,
        string sensorId,
        AttackType attackType,
        AttackProtocol protocol,
        AttackSeverity severity,
        GeoLocation geolocation,
        long sessionId,
        DateTime receivedAt,
        bool wasEdgeFiltered,
        string rawDataJson,
        bool isAnalyzed,
        decimal threatScore,
        AttackIntent intent,
        AttackCredentials? credentials = null,
        string? command = null,
        byte[]? payload = null,
        string? fileHash = null,
        string? userAgent = null,
        Dictionary<string, string>? headers = null,
        List<MitreTechnique>? mitreTechniques = null,
        bool isAnomaly = false,
        Guid? threatActorId = null)
    {
        var attackEvent = new AttackEvent(id, honeypotId, organizationId, externalEventId)
        {
            Timestamp = timestamp,
            SourceEndpoint = sourceEndpoint,
            TargetEndpoint = targetEndpoint,
            SensorId = sensorId,
            AttackType = attackType,
            Protocol = protocol,
            Severity = severity,
            Geolocation = geolocation,
            SessionId = sessionId,
            ReceivedAt = receivedAt,
            WasEdgeFiltered = wasEdgeFiltered,
            RawDataJson = rawDataJson,
            IsAnalyzed = isAnalyzed,
            ThreatScore = threatScore,
            Intent = intent,
            Credentials = credentials,
            Command = command,
            Payload = payload,
            FileHash = fileHash,
            UserAgent = userAgent,
            IsAnomaly = isAnomaly,
            ThreatActorId = threatActorId
        };

        if (headers != null)
            attackEvent._headers = headers;

        if (mitreTechniques != null)
            attackEvent._mitreTechniques = mitreTechniques;

        return attackEvent;
    }

    #endregion

    #region Domain Behaviors

    /// <summary>
    /// Complete AI analysis (called asynchronously after creation)
    /// </summary>
    public Result CompleteAIAnalysis(
        decimal threatScore,
        AttackIntent intent,
        List<MitreTechnique> mitreTechniques,
        AttackSeverity? updatedSeverity = null,
        bool isAnomaly = false)
    {
        if (IsAnalyzed)
            return Result.Failure(AttackErrors.AlreadyAnalyzed);

        if (threatScore < 0 || threatScore > 100)
            return Result.Failure(AttackErrors.InvalidThreatScore);

        ThreatScore = threatScore;
        Intent = intent;
        _mitreTechniques = mitreTechniques ?? new List<MitreTechnique>();
        IsAnomaly = isAnomaly;
        IsAnalyzed = true;

        // AI can upgrade severity
        if (updatedSeverity.HasValue && updatedSeverity.Value > Severity)
        {
            Severity = updatedSeverity.Value;
        }

        RaiseDomainEvent(new AttackEventAnalyzedEvent(
            Id,
            HoneypotId,
            OrganizationId,
            ThreatScore,
            Intent,
            DateTime.UtcNow));

        // If upgraded to high severity, raise alert
        if (Severity == AttackSeverity.High || Severity == AttackSeverity.Critical)
        {
            RaiseDomainEvent(new HighSeverityAttackDetectedEvent(
                Id,
                HoneypotId,
                OrganizationId,
                SourceEndpoint.IPAddress,
                Severity,
                ThreatScore,
                DateTime.UtcNow));
        }

        return Result.Success();
    }

    /// <summary>
    /// Link to threat actor (after correlation)
    /// </summary>
    public Result LinkToThreatActor(Guid threatActorId)
    {
        if (threatActorId == Guid.Empty)
            return Result.Failure(
                Error.Custom("AttackEvent.InvalidThreatActorId", "Threat actor ID cannot be empty"));

        ThreatActorId = threatActorId;

        RaiseDomainEvent(new AttackEventLinkedToThreatActorEvent(
            Id,
            threatActorId,
            HoneypotId,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Mark as anomaly (unusual behavior detected)
    /// </summary>
    public Result MarkAsAnomaly()
    {
        if (IsAnomaly)
            return Result.Failure(AttackErrors.AlreadyMarkedAsAnomaly);

        IsAnomaly = true;

        RaiseDomainEvent(new AttackEventMarkedAsAnomalyEvent(
            Id,
            HoneypotId,
            OrganizationId,
            SourceEndpoint.IPAddress,
            AttackType,
            DateTime.UtcNow));

        return Result.Success();
    }

    #endregion

    #region Query Helpers - Uses AttackEventSeverityPolicy

    /// <summary>
    /// Check if high severity.
    /// Delegates to AttackEventSeverityPolicy.
    /// </summary>
    public bool IsHighSeverity() =>
        Policies.AttackEventSeverityPolicy.IsHighSeverity(Severity);

    /// <summary>
    /// Check if contains malware
    /// </summary>
    public bool HasMalware() => Payload != null && FileHash != null;

    /// <summary>
    /// Check if credentials were attempted
    /// </summary>
    public bool HasCredentials() => Credentials != null;

    /// <summary>
    /// Check if attack should trigger alert.
    /// Delegates to AttackEventSeverityPolicy.
    /// </summary>
    public bool ShouldTriggerAlert() =>
        Policies.AttackEventSeverityPolicy.ShouldTriggerAlert(Severity, ThreatScore, HasMalware());

    /// <summary>
    /// Check if attack is brute force variant.
    /// Delegates to AttackEventSeverityPolicy.
    /// </summary>
    public bool IsBruteForceAttack() =>
        Policies.AttackEventSeverityPolicy.IsBruteForceAttack(AttackType);

    /// <summary>
    /// Get severity weight for sorting.
    /// Delegates to AttackEventSeverityPolicy.
    /// </summary>
    public int GetSeverityWeight() =>
        Policies.AttackEventSeverityPolicy.GetSeverityWeight(Severity);

    /// <summary>
    /// Get parsed raw data
    /// </summary>
    public Result<Dictionary<string, object>> GetParsedRawData()
    {
        if (!HasRawData)
            return Result.Failure<Dictionary<string, object>>(
                Error.Custom("AttackEvent.NoRawData", "No raw data available"));

        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, object>>(RawDataJson);
            return Result.Success(parsed ?? new Dictionary<string, object>());
        }
        catch (JsonException ex)
        {
            return Result.Failure<Dictionary<string, object>>(
                Error.Custom("AttackEvent.InvalidJson", ex.Message));
        }
    }

    #endregion

    #region Private Helpers - Delegates to AttackEventMappingPolicy

    private static AttackType MapAttackType(string eventType) =>
        Policies.AttackEventMappingPolicy.MapAttackType(eventType);

    private static AttackProtocol MapProtocol(string protocol) =>
        Policies.AttackEventMappingPolicy.MapProtocol(protocol);

    private static AttackSeverity MapSeverity(string severity) =>
        Policies.AttackEventMappingPolicy.MapSeverity(severity);

    private static string ComputeFileHash(byte[] data) =>
        Policies.AttackEventMappingPolicy.ComputeFileHash(data);

    #endregion
}
