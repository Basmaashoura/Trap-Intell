namespace Trap_Intel.Domain.ThreatActors.Enums;

/// <summary>
/// Threat level classification for actors.
/// Based on observed behavior and potential impact.
/// </summary>
public enum ThreatLevel
{
    Unknown = 0,
    Low = 1,            // Script kiddies, automated scanners
    Medium = 2,         // Opportunistic attackers, botnets
    High = 3,           // Skilled attackers, targeted attempts
    Critical = 4,       // APT, nation-state, sophisticated actors
    Severe = 5          // Active breach, immediate response required
}

/// <summary>
/// Classification of threat actor type.
/// </summary>
public enum ThreatActorType
{
    Unknown = 0,
    ScriptKiddie = 1,           // Low-skill automated attacks
    Botnet = 2,                 // Part of botnet infrastructure
    OpportunisticAttacker = 3,  // Scanning for easy targets
    CybercriminalGroup = 4,     // Organized crime
    Hacktivist = 5,             // Politically motivated
    NationState = 6,            // State-sponsored
    InsiderThreat = 7,          // Internal actor
    APT = 8,                    // Advanced Persistent Threat
    Researcher = 9              // Security researcher (benign)
}

/// <summary>
/// Status of threat actor profile.
/// </summary>
public enum ThreatActorStatus
{
    Active = 0,             // Currently active, recent attacks
    Dormant = 1,            // No recent activity
    Blocked = 2,            // All IPs blocked
    Monitored = 3,          // Under surveillance
    Neutralized = 4,        // Threat eliminated
    FalsePositive = 5       // Determined to be benign
}

/// <summary>
/// Confidence level of threat actor identification.
/// </summary>
public enum IdentificationConfidence
{
    Low = 0,        // Single attack, limited data
    Medium = 1,     // Multiple attacks, some patterns
    High = 2,       // Strong patterns, consistent behavior
    Confirmed = 3   // Verified through multiple sources
}

/// <summary>
/// Geographic region for threat origin tracking.
/// </summary>
public enum ThreatRegion
{
    Unknown = 0,
    NorthAmerica = 1,
    SouthAmerica = 2,
    WesternEurope = 3,
    EasternEurope = 4,
    Russia = 5,
    MiddleEast = 6,
    Africa = 7,
    SouthAsia = 8,
    EastAsia = 9,
    SoutheastAsia = 10,
    Oceania = 11,
    Tor = 12,           // TOR exit nodes
    VPN = 13,           // Known VPN services
    CloudProvider = 14  // Cloud infrastructure
}

/// <summary>
/// Primary motivation of threat actor.
/// </summary>
public enum ThreatMotivation
{
    Unknown = 0,
    FinancialGain = 1,      // Ransomware, theft
    Espionage = 2,          // Data theft, surveillance
    Disruption = 3,         // DDoS, sabotage
    Hacktivism = 4,         // Political/social cause
    Reconnaissance = 5,     // Information gathering
    Testing = 6,            // Security testing
    Curiosity = 7           // Learning/exploration
}
