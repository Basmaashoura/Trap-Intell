namespace Trap_Intel.Domain.Webhooks.Enums;

/// <summary>
/// Status of a webhook.
/// </summary>
public enum WebhookStatus
{
    /// <summary>Webhook is active and receiving events.</summary>
    Active = 0,
    
    /// <summary>Webhook is manually disabled.</summary>
    Disabled = 1,
    
    /// <summary>Webhook was auto-disabled due to consecutive failures.</summary>
    DisabledByFailures = 2,
    
    /// <summary>Webhook is deleted (soft delete).</summary>
    Deleted = 3
}

/// <summary>
/// Content type for webhook delivery.
/// </summary>
public enum WebhookContentType
{
    /// <summary>JSON content type (application/json).</summary>
    Json = 0,
    
    /// <summary>Form URL encoded (application/x-www-form-urlencoded).</summary>
    Form = 1
}

/// <summary>
/// Event types that can trigger webhooks.
/// </summary>
[Flags]
public enum WebhookEventType
{
    None = 0,
    
    // Attack events
    AttackDetected = 1 << 0,
    HighSeverityAttack = 1 << 1,
    MalwareDetected = 1 << 2,
    
    // Alert events
    AlertCreated = 1 << 3,
    AlertResolved = 1 << 4,
    AlertEscalated = 1 << 5,
    
    // Threat actor events
    ThreatActorIdentified = 1 << 6,
    ThreatLevelEscalated = 1 << 7,
    
    // Honeypot events
    HoneypotDeployed = 1 << 8,
    HoneypotOffline = 1 << 9,
    HoneypotHealthCritical = 1 << 10,
    
    // System events
    QuotaWarning = 1 << 11,
    QuotaExceeded = 1 << 12,
    SubscriptionExpiring = 1 << 13,
    
    // Security events
    ApiKeyUsed = 1 << 14,
    SuspiciousActivity = 1 << 15,
    
    // Report events
    ReportGenerated = 1 << 16,
    
    // All events
    All = int.MaxValue
}
