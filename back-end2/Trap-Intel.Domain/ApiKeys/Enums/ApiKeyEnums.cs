namespace Trap_Intel.Domain.ApiKeys.Enums;

/// <summary>
/// Status of an API key.
/// </summary>
public enum ApiKeyStatus
{
    /// <summary>Key is active and can be used.</summary>
    Active = 0,
    
    /// <summary>Key is temporarily suspended.</summary>
    Suspended = 1,
    
    /// <summary>Key has been permanently revoked.</summary>
    Revoked = 2,
    
    /// <summary>Key has expired.</summary>
    Expired = 3
}

/// <summary>
/// Type of API key.
/// </summary>
public enum ApiKeyType
{
    /// <summary>Live production key with full access.</summary>
    Live = 0,
    
    /// <summary>Test key for development/sandbox.</summary>
    Test = 1,
    
    /// <summary>Read-only key (no mutations).</summary>
    ReadOnly = 2,
    
    /// <summary>Webhook-specific key (limited scope).</summary>
    Webhook = 3,
    
    /// <summary>Service account key (machine-to-machine).</summary>
    ServiceAccount = 4
}

/// <summary>
/// Permissions that can be granted to an API key.
/// </summary>
[Flags]
public enum ApiKeyPermission
{
    None = 0,
    
    // Honeypot permissions
    ReadHoneypots = 1 << 0,
    WriteHoneypots = 1 << 1,
    ManageHoneypots = 1 << 2,
    
    // Attack event permissions
    ReadAttacks = 1 << 3,
    ExportAttacks = 1 << 4,
    
    // Threat actor permissions
    ReadThreatActors = 1 << 5,
    WriteThreatActors = 1 << 6,
    
    // Alert permissions
    ReadAlerts = 1 << 7,
    WriteAlerts = 1 << 8,
    ManageAlerts = 1 << 9,
    
    // Report permissions
    ReadReports = 1 << 10,
    GenerateReports = 1 << 11,
    
    // Organization permissions (admin)
    ReadOrganization = 1 << 12,
    ManageOrganization = 1 << 13,
    
    // User permissions (admin)
    ReadUsers = 1 << 14,
    ManageUsers = 1 << 15,
    
    // Subscription permissions (admin)
    ReadSubscription = 1 << 16,
    ManageSubscription = 1 << 17,
    
    // Webhook permissions
    ManageWebhooks = 1 << 18,
    
    // API key management
    ManageApiKeys = 1 << 19,
    
    // Command permissions
    SendCommands = 1 << 20,
    
    // Full access (all permissions)
    FullAccess = int.MaxValue
}

/// <summary>
/// Scope for rate limiting.
/// </summary>
public enum RateLimitScope
{
    /// <summary>Rate limit per minute.</summary>
    PerMinute = 0,
    
    /// <summary>Rate limit per hour.</summary>
    PerHour = 1,
    
    /// <summary>Rate limit per day.</summary>
    PerDay = 2,
    
    /// <summary>Rate limit per month.</summary>
    PerMonth = 3
}
