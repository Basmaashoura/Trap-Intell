namespace Trap_Intel.Domain.Attacks.Enums;

/// <summary>
/// Type of attack captured by honeypot
/// </summary>
public enum AttackType
{
    Unknown = 0,
    SSHBruteForce = 1,
    HTTPExploit = 2,
    SQLInjection = 3,
    MalwareUpload = 4,
    PortScan = 5,
    FTPBruteForce = 6,
    SMTPSpam = 7,
    DNSAmplification = 8,
    RDPBruteForce = 9,
    TelnetBruteForce = 10,
    WebShell = 11,
    CommandInjection = 12,
    FileInclusion = 13,
    CrossSiteScripting = 14,
    BufferOverflow = 15
}

/// <summary>
/// Severity of attack (can be updated by AI)
/// </summary>
public enum AttackSeverity
{
    Info = 0,        // Benign reconnaissance
    Low = 1,         // Script kiddie attempts
    Medium = 2,      // Automated scanning
    High = 3,        // Targeted exploitation
    Critical = 4     // Active breach attempt or malware
}

/// <summary>
/// Intent behind attack (AI-classified)
/// </summary>
public enum AttackIntent
{
    Unknown = 0,
    Reconnaissance = 1,      // Scanning, probing
    Exploitation = 2,        // Attempting exploits
    Persistence = 3,         // Establishing backdoor
    PrivilegeEscalation = 4, // Gaining higher privileges
    DefenseEvasion = 5,      // Hiding tracks
    CredentialAccess = 6,    // Stealing credentials
    DataExfiltration = 7,    // Stealing data
    Impact = 8               // Ransomware, destruction
}

/// <summary>
/// Protocol used in attack
/// </summary>
public enum AttackProtocol
{
    Unknown = 0,
    SSH = 1,
    HTTP = 2,
    HTTPS = 3,
    FTP = 4,
    SMTP = 5,
    DNS = 6,
    RDP = 7,
    Telnet = 8,
    SMB = 9,
    MySQL = 10,
    PostgreSQL = 11,
    Redis = 12
}
