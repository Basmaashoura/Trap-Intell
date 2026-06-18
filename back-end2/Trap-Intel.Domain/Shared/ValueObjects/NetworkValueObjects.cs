using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Shared.ValueObjects;

/// <summary>
/// Value object representing a validated IP address (IPv4 or IPv6).
/// Provides validation, parsing, and utility methods.
/// </summary>
public partial record IPAddressVO
{
    /// <summary>
    /// The string representation of the IP address.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// The IP version (4 or 6).
    /// </summary>
    public int Version { get; }

    /// <summary>
    /// Whether this is a private/internal IP address.
    /// </summary>
    public bool IsPrivate { get; }

    /// <summary>
    /// Whether this is a loopback address.
    /// </summary>
    public bool IsLoopback { get; }

    /// <summary>
    /// The parsed IPAddress object.
    /// </summary>
    private readonly IPAddress _ipAddress;

    private IPAddressVO(string value, IPAddress ipAddress)
    {
        Value = value;
        _ipAddress = ipAddress;
        Version = ipAddress.AddressFamily == AddressFamily.InterNetwork ? 4 : 6;
        IsPrivate = CheckIsPrivate(ipAddress);
        IsLoopback = IPAddress.IsLoopback(ipAddress);
    }

    /// <summary>
    /// Create a validated IP address.
    /// </summary>
    public static Result<IPAddressVO> Create(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return Result.Failure<IPAddressVO>(
                Error.Custom("IPAddress.Empty", "IP address cannot be empty"));

        var trimmed = ipAddress.Trim();

        if (!IPAddress.TryParse(trimmed, out var parsed))
            return Result.Failure<IPAddressVO>(
                Error.Custom("IPAddress.InvalidFormat", $"'{trimmed}' is not a valid IP address"));

        // Normalize the IP address representation
        var normalized = parsed.ToString();

        return Result.Success(new IPAddressVO(normalized, parsed));
    }

    /// <summary>
    /// Create without validation (for trusted sources).
    /// </summary>
    public static IPAddressVO CreateUnsafe(string ipAddress)
    {
        var parsed = IPAddress.Parse(ipAddress.Trim());
        return new IPAddressVO(parsed.ToString(), parsed);
    }

    /// <summary>
    /// Try to create an IP address.
    /// </summary>
    public static bool TryCreate(string ipAddress, out IPAddressVO? result)
    {
        var createResult = Create(ipAddress);
        if (createResult.IsSuccess)
        {
            result = createResult.Value;
            return true;
        }

        result = null;
        return false;
    }

    /// <summary>
    /// Check if this IP is in a CIDR range.
    /// </summary>
    public bool IsInRange(string cidr)
    {
        if (string.IsNullOrWhiteSpace(cidr))
            return false;

        try
        {
            var parts = cidr.Split('/');
            if (parts.Length != 2)
                return false;

            if (!IPAddress.TryParse(parts[0], out var networkAddress))
                return false;

            if (!int.TryParse(parts[1], out var prefixLength))
                return false;

            // Ensure same address family
            if (networkAddress.AddressFamily != _ipAddress.AddressFamily)
                return false;

            var networkBytes = networkAddress.GetAddressBytes();
            var addressBytes = _ipAddress.GetAddressBytes();

            var bytesToCheck = prefixLength / 8;
            var bitsToCheck = prefixLength % 8;

            // Check full bytes
            for (int i = 0; i < bytesToCheck; i++)
            {
                if (networkBytes[i] != addressBytes[i])
                    return false;
            }

            // Check remaining bits
            if (bitsToCheck > 0 && bytesToCheck < networkBytes.Length)
            {
                var mask = (byte)(0xFF << (8 - bitsToCheck));
                if ((networkBytes[bytesToCheck] & mask) != (addressBytes[bytesToCheck] & mask))
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Check if this is a public (routable) IP address.
    /// </summary>
    public bool IsPublic => !IsPrivate && !IsLoopback && !IsReserved();

    /// <summary>
    /// Check if this is a reserved address.
    /// </summary>
    public bool IsReserved()
    {
        var bytes = _ipAddress.GetAddressBytes();

        if (Version == 4)
        {
            // 0.0.0.0/8 - Current network
            if (bytes[0] == 0)
                return true;

            // 100.64.0.0/10 - Shared address space (CGNAT)
            if (bytes[0] == 100 && (bytes[1] & 0xC0) == 64)
                return true;

            // 169.254.0.0/16 - Link-local
            if (bytes[0] == 169 && bytes[1] == 254)
                return true;

            // 192.0.0.0/24 - IETF Protocol Assignments
            if (bytes[0] == 192 && bytes[1] == 0 && bytes[2] == 0)
                return true;

            // 192.0.2.0/24 - Documentation (TEST-NET-1)
            if (bytes[0] == 192 && bytes[1] == 0 && bytes[2] == 2)
                return true;

            // 198.51.100.0/24 - Documentation (TEST-NET-2)
            if (bytes[0] == 198 && bytes[1] == 51 && bytes[2] == 100)
                return true;

            // 203.0.113.0/24 - Documentation (TEST-NET-3)
            if (bytes[0] == 203 && bytes[1] == 0 && bytes[2] == 113)
                return true;

            // 224.0.0.0/4 - Multicast
            if ((bytes[0] & 0xF0) == 224)
                return true;

            // 240.0.0.0/4 - Reserved
            if ((bytes[0] & 0xF0) == 240)
                return true;

            // 255.255.255.255 - Broadcast
            if (bytes[0] == 255 && bytes[1] == 255 && bytes[2] == 255 && bytes[3] == 255)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Get the IP address bytes.
    /// </summary>
    public byte[] GetBytes() => _ipAddress.GetAddressBytes();

    /// <summary>
    /// Convert to System.Net.IPAddress.
    /// </summary>
    public IPAddress ToIPAddress() => _ipAddress;

    public override string ToString() => Value;

    /// <summary>
    /// Implicit conversion to string.
    /// </summary>
    public static implicit operator string(IPAddressVO ip) => ip.Value;

    private static bool CheckIsPrivate(IPAddress ipAddress)
    {
        var bytes = ipAddress.GetAddressBytes();

        if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
        {
            // IPv4 private ranges
            // 10.0.0.0/8
            if (bytes[0] == 10)
                return true;

            // 172.16.0.0/12
            if (bytes[0] == 172 && (bytes[1] >= 16 && bytes[1] <= 31))
                return true;

            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168)
                return true;
        }
        else if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
        {
            // IPv6 unique local addresses (fc00::/7)
            if ((bytes[0] & 0xFE) == 0xFC)
                return true;

            // IPv6 link-local (fe80::/10)
            if (bytes[0] == 0xFE && (bytes[1] & 0xC0) == 0x80)
                return true;
        }

        return false;
    }
}

/// <summary>
/// Value object representing a validated port number.
/// </summary>
public record PortVO
{
    /// <summary>
    /// The port number.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Whether this is a well-known port (0-1023).
    /// </summary>
    public bool IsWellKnown => Value >= 0 && Value <= 1023;

    /// <summary>
    /// Whether this is a registered port (1024-49151).
    /// </summary>
    public bool IsRegistered => Value >= 1024 && Value <= 49151;

    /// <summary>
    /// Whether this is a dynamic/ephemeral port (49152-65535).
    /// </summary>
    public bool IsDynamic => Value >= 49152 && Value <= 65535;

    private PortVO(int value)
    {
        Value = value;
    }

    /// <summary>
    /// Create a validated port.
    /// </summary>
    public static Result<PortVO> Create(int port)
    {
        if (port < 0 || port > 65535)
            return Result.Failure<PortVO>(
                Error.Custom("Port.OutOfRange", $"Port {port} must be between 0 and 65535"));

        return Result.Success(new PortVO(port));
    }

    /// <summary>
    /// Create without validation.
    /// </summary>
    public static PortVO CreateUnsafe(int port) => new(port);

    /// <summary>
    /// Try to create a port.
    /// </summary>
    public static bool TryCreate(int port, out PortVO? result)
    {
        var createResult = Create(port);
        if (createResult.IsSuccess)
        {
            result = createResult.Value;
            return true;
        }

        result = null;
        return false;
    }

    /// <summary>
    /// Common well-known ports.
    /// </summary>
    public static class WellKnown
    {
        public static readonly PortVO FTP = CreateUnsafe(21);
        public static readonly PortVO SSH = CreateUnsafe(22);
        public static readonly PortVO Telnet = CreateUnsafe(23);
        public static readonly PortVO SMTP = CreateUnsafe(25);
        public static readonly PortVO DNS = CreateUnsafe(53);
        public static readonly PortVO HTTP = CreateUnsafe(80);
        public static readonly PortVO POP3 = CreateUnsafe(110);
        public static readonly PortVO IMAP = CreateUnsafe(143);
        public static readonly PortVO HTTPS = CreateUnsafe(443);
        public static readonly PortVO SMB = CreateUnsafe(445);
        public static readonly PortVO MySQL = CreateUnsafe(3306);
        public static readonly PortVO PostgreSQL = CreateUnsafe(5432);
        public static readonly PortVO RDP = CreateUnsafe(3389);
        public static readonly PortVO Redis = CreateUnsafe(6379);
        public static readonly PortVO MongoDB = CreateUnsafe(27017);
    }

    public override string ToString() => Value.ToString();

    /// <summary>
    /// Implicit conversion to int.
    /// </summary>
    public static implicit operator int(PortVO port) => port.Value;
}

/// <summary>
/// Value object representing a CIDR notation IP range.
/// </summary>
public record CIDRRange
{
    /// <summary>
    /// The CIDR notation string (e.g., "192.168.1.0/24").
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// The network address.
    /// </summary>
    public IPAddressVO NetworkAddress { get; }

    /// <summary>
    /// The prefix length (subnet mask bits).
    /// </summary>
    public int PrefixLength { get; }

    /// <summary>
    /// The IP version (4 or 6).
    /// </summary>
    public int Version => NetworkAddress.Version;

    private CIDRRange(string value, IPAddressVO networkAddress, int prefixLength)
    {
        Value = value;
        NetworkAddress = networkAddress;
        PrefixLength = prefixLength;
    }

    /// <summary>
    /// Create a validated CIDR range.
    /// </summary>
    public static Result<CIDRRange> Create(string cidr)
    {
        if (string.IsNullOrWhiteSpace(cidr))
            return Result.Failure<CIDRRange>(
                Error.Custom("CIDR.Empty", "CIDR range cannot be empty"));

        var parts = cidr.Trim().Split('/');
        if (parts.Length != 2)
            return Result.Failure<CIDRRange>(
                Error.Custom("CIDR.InvalidFormat", "CIDR must be in format 'address/prefix'"));

        var ipResult = IPAddressVO.Create(parts[0]);
        if (ipResult.IsFailure)
            return Result.Failure<CIDRRange>(ipResult.Errors[0]);

        if (!int.TryParse(parts[1], out var prefixLength))
            return Result.Failure<CIDRRange>(
                Error.Custom("CIDR.InvalidPrefix", "Prefix length must be a number"));

        var maxPrefix = ipResult.Value.Version == 4 ? 32 : 128;
        if (prefixLength < 0 || prefixLength > maxPrefix)
            return Result.Failure<CIDRRange>(
                Error.Custom("CIDR.PrefixOutOfRange", $"Prefix length must be between 0 and {maxPrefix}"));

        var normalized = $"{ipResult.Value.Value}/{prefixLength}";
        return Result.Success(new CIDRRange(normalized, ipResult.Value, prefixLength));
    }

    /// <summary>
    /// Check if an IP address is in this range.
    /// </summary>
    public bool Contains(IPAddressVO ipAddress)
    {
        return ipAddress.IsInRange(Value);
    }

    /// <summary>
    /// Check if an IP address string is in this range.
    /// </summary>
    public bool Contains(string ipAddress)
    {
        var result = IPAddressVO.Create(ipAddress);
        return result.IsSuccess && Contains(result.Value);
    }

    public override string ToString() => Value;
}
