using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Attacks.ValueObjects;

/// <summary>
/// Geolocation of attacker
/// </summary>
public record GeoLocation
{
    public string Country { get; init; }
    public string CountryCode { get; init; }
    public string City { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public string Region { get; init; }
    public string ISP { get; init; }
    public string ASN { get; init; }

    public static GeoLocation Unknown() => new()
    {
        Country = "Unknown",
        CountryCode = "XX",
        City = "Unknown"
    };

    public static Result<GeoLocation> Create(
        string country,
        string countryCode,
        string city,
        decimal? latitude = null,
        decimal? longitude = null,
        string region = null,
        string isp = null,
        string asn = null)
    {
        if (string.IsNullOrWhiteSpace(country))
            return Result.Failure<GeoLocation>(
                Error.Custom("GeoLocation.InvalidCountry", "Country cannot be empty"));

        return Result.Success(new GeoLocation
        {
            Country = country,
            CountryCode = countryCode ?? "XX",
            City = city ?? "Unknown",
            Latitude = latitude,
            Longitude = longitude,
            Region = region,
            ISP = isp,
            ASN = asn
        });
    }
}

/// <summary>
/// Network endpoint (IP + Port)
/// </summary>
public record NetworkEndpoint
{
    public string IPAddress { get; init; }
    public int Port { get; init; }

    public static Result<NetworkEndpoint> Create(string ipAddress, int port)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return Result.Failure<NetworkEndpoint>(
                Error.Custom("NetworkEndpoint.InvalidIP", "IP address cannot be empty"));

        if (port < 0 || port > 65535)
            return Result.Failure<NetworkEndpoint>(
                Error.Custom("NetworkEndpoint.InvalidPort", "Port must be between 0 and 65535"));

        return Result.Success(new NetworkEndpoint
        {
            IPAddress = ipAddress,
            Port = port
        });
    }

    public override string ToString() => $"{IPAddress}:{Port}";
}

/// <summary>
/// Attack credentials (username + password attempted)
/// </summary>
public record AttackCredentials
{
    public string Username { get; init; }
    public string Password { get; init; }
    public string PasswordHash { get; init; }  // SHA256 hash for privacy

    public static AttackCredentials Create(string username, string password)
    {
        return new AttackCredentials
        {
            Username = username ?? string.Empty,
            Password = password ?? string.Empty,
            PasswordHash = HashPassword(password)
        };
    }

    private static string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return string.Empty;

        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}

/// <summary>
/// MITRE ATT&CK technique
/// </summary>
public record MitreTechnique
{
    public string TechniqueId { get; init; }    // e.g., "T1110"
    public string TechniqueName { get; init; }  // e.g., "Brute Force"
    public string TacticName { get; init; }     // e.g., "Credential Access"

    public static Result<MitreTechnique> Create(string techniqueId, string techniqueName, string tacticName)
    {
        if (string.IsNullOrWhiteSpace(techniqueId))
            return Result.Failure<MitreTechnique>(
                Error.Custom("MitreTechnique.InvalidId", "Technique ID cannot be empty"));

        return Result.Success(new MitreTechnique
        {
            TechniqueId = techniqueId,
            TechniqueName = techniqueName ?? "Unknown",
            TacticName = tacticName ?? "Unknown"
        });
    }
}
