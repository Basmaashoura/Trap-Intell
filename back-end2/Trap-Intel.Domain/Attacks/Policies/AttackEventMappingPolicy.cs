using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Attacks.Enums;
using Trap_Intel.Domain.Attacks.ValueObjects;

namespace Trap_Intel.Domain.Attacks.Policies;

/// <summary>
/// Policy for mapping external data to domain types.
/// Separates mapping/parsing logic from aggregate.
/// </summary>
public static class AttackEventMappingPolicy
{
    /// <summary>
    /// Map event type string to AttackType enum.
    /// </summary>
    public static AttackType MapAttackType(string? eventType)
    {
        return eventType?.ToLower() switch
        {
            "ssh_login_attempt" or "ssh_brute_force" => AttackType.SSHBruteForce,
            "http_exploit" or "web_exploit" => AttackType.HTTPExploit,
            "sql_injection" => AttackType.SQLInjection,
            "malware_upload" or "file_upload" => AttackType.MalwareUpload,
            "port_scan" => AttackType.PortScan,
            "ftp_brute_force" => AttackType.FTPBruteForce,
            "rdp_brute_force" => AttackType.RDPBruteForce,
            "telnet_brute_force" => AttackType.TelnetBruteForce,
            "web_shell" => AttackType.WebShell,
            "command_injection" => AttackType.CommandInjection,
            "xss" or "cross_site_scripting" => AttackType.CrossSiteScripting,
            _ => AttackType.Unknown
        };
    }

    /// <summary>
    /// Map protocol string to AttackProtocol enum.
    /// </summary>
    public static AttackProtocol MapProtocol(string? protocol)
    {
        return protocol?.ToLower() switch
        {
            "ssh" => AttackProtocol.SSH,
            "http" => AttackProtocol.HTTP,
            "https" => AttackProtocol.HTTPS,
            "ftp" => AttackProtocol.FTP,
            "smtp" => AttackProtocol.SMTP,
            "dns" => AttackProtocol.DNS,
            "rdp" => AttackProtocol.RDP,
            "telnet" => AttackProtocol.Telnet,
            "smb" => AttackProtocol.SMB,
            "mysql" => AttackProtocol.MySQL,
            "postgresql" => AttackProtocol.PostgreSQL,
            _ => AttackProtocol.Unknown
        };
    }

    /// <summary>
    /// Map severity string to AttackSeverity enum.
    /// </summary>
    public static AttackSeverity MapSeverity(string? severity)
    {
        return severity?.ToLower() switch
        {
            "info" => AttackSeverity.Info,
            "low" => AttackSeverity.Low,
            "medium" => AttackSeverity.Medium,
            "high" => AttackSeverity.High,
            "critical" => AttackSeverity.Critical,
            _ => AttackSeverity.Low
        };
    }

    /// <summary>
    /// Create geolocation from interface data.
    /// </summary>
    public static GeoLocation MapGeolocation(IGeoLocationData? data)
    {
        if (data == null)
            return GeoLocation.Unknown();

        return GeoLocation.Create(
            data.Country ?? "Unknown",
            data.CountryCode ?? "XX",
            data.City ?? "Unknown",
            data.Latitude,
            data.Longitude,
            data.Region,
            data.ISP,
            data.ASN).Value;
    }

    /// <summary>
    /// Compute SHA256 hash of file data.
    /// </summary>
    public static string ComputeFileHash(byte[] data)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return Convert.ToHexString(hash).ToLower();
    }

    /// <summary>
    /// Validate attack event data for required fields.
    /// </summary>
    public static Result ValidateAttackEventData(IAttackEventData? data)
    {
        if (data == null)
            return Result.Failure(AttackErrors.InvalidData);

        if (string.IsNullOrWhiteSpace(data.ExternalEventId))
            return Result.Failure(
                Error.Custom("AttackEvent.MissingExternalId", "External event ID is required"));

        if (string.IsNullOrWhiteSpace(data.SourceIP))
            return Result.Failure(
                Error.Custom("AttackEvent.MissingSourceIP", "Source IP is required"));

        return Result.Success();
    }
}
