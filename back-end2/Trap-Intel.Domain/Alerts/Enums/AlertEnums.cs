namespace Trap_Intel.Domain.Alerts.Enums;

/// <summary>
/// Alert severity level.
/// </summary>
public enum AlertSeverity
{
    Info = 0,           // Informational
    Low = 1,            // Low priority
    Medium = 2,         // Standard priority
    High = 3,           // Urgent attention needed
    Critical = 4        // Immediate action required
}

/// <summary>
/// Alert status.
/// </summary>
public enum AlertStatus
{
    New = 0,                // Just created, not seen
    Acknowledged = 1,       // Seen by analyst
    InProgress = 2,         // Being investigated
    Escalated = 3,          // Escalated to higher level
    Resolved = 4,           // Issue resolved
    FalsePositive = 5,      // Not a real threat
    Snoozed = 6,            // Temporarily dismissed
    Expired = 7             // Auto-closed due to age
}

/// <summary>
/// Type of alert.
/// </summary>
public enum AlertType
{
    // Attack Alerts
    HighSeverityAttack = 0,
    MalwareDetected = 1,
    BruteForceAttempt = 2,
    SQLInjectionAttempt = 3,
    CommandInjectionAttempt = 4,
    
    // Threat Actor Alerts
    NewThreatActor = 10,
    ThreatLevelEscalation = 11,
    APTActivity = 12,
    
    // Honeypot Alerts
    HoneypotOffline = 20,
    HoneypotHealthCritical = 21,
    HoneypotStorageNearLimit = 22,
    
    // System Alerts
    SystemPerformanceIssue = 30,
    QuotaExceeded = 31,
    SubscriptionExpiring = 32,
    
    // Security Alerts
    SuspiciousActivity = 40,
    AnomalyDetected = 41,
    
    // Custom
    Custom = 99
}

/// <summary>
/// Notification channel for alerts.
/// </summary>
public enum NotificationChannel
{
    None = 0,
    Dashboard = 1,      // In-app notification
    Email = 2,          // Email notification
    SMS = 3,            // SMS notification
    Slack = 4,          // Slack webhook
    Teams = 5,          // Microsoft Teams
    Webhook = 6,        // Generic webhook
    PagerDuty = 7,      // PagerDuty integration
    All = 99            // All channels
}

/// <summary>
/// Priority for alert handling.
/// </summary>
public enum AlertPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3,
    Emergency = 4
}

/// <summary>
/// Escalation level.
/// </summary>
public enum EscalationLevel
{
    Level1 = 1,     // First responder / analyst
    Level2 = 2,     // Senior analyst
    Level3 = 3,     // Security manager
    Level4 = 4,     // CISO / executive
    External = 5    // External response team
}
