namespace Trap_Intel.Domain.ApiKeys.ValueObjects;

/// <summary>
/// Rate limit configuration for an API key.
/// </summary>
public record ApiKeyRateLimit
{
    /// <summary>
    /// Maximum requests per minute.
    /// </summary>
    public int RequestsPerMinute { get; init; }

    /// <summary>
    /// Maximum requests per hour.
    /// </summary>
    public int RequestsPerHour { get; init; }

    /// <summary>
    /// Maximum requests per day.
    /// </summary>
    public int RequestsPerDay { get; init; }

    /// <summary>
    /// Whether rate limiting is enabled.
    /// </summary>
    public bool IsEnabled { get; init; }

    /// <summary>
    /// Create rate limit configuration.
    /// </summary>
    public ApiKeyRateLimit(
        int requestsPerMinute = 60,
        int requestsPerHour = 1000,
        int requestsPerDay = 10000,
        bool isEnabled = true)
    {
        RequestsPerMinute = requestsPerMinute > 0 ? requestsPerMinute : 60;
        RequestsPerHour = requestsPerHour > 0 ? requestsPerHour : 1000;
        RequestsPerDay = requestsPerDay > 0 ? requestsPerDay : 10000;
        IsEnabled = isEnabled;
    }

    /// <summary>
    /// Default rate limit (60/min, 1000/hour, 10000/day).
    /// </summary>
    public static ApiKeyRateLimit Default() => new();

    /// <summary>
    /// Low rate limit for restricted keys.
    /// </summary>
    public static ApiKeyRateLimit Low() => new(10, 100, 1000);

    /// <summary>
    /// High rate limit for premium access.
    /// </summary>
    public static ApiKeyRateLimit High() => new(300, 10000, 100000);

    /// <summary>
    /// Unlimited (rate limiting disabled).
    /// </summary>
    public static ApiKeyRateLimit Unlimited() => new(int.MaxValue, int.MaxValue, int.MaxValue, false);

    /// <summary>
    /// Test key rate limit (very low).
    /// </summary>
    public static ApiKeyRateLimit TestKey() => new(5, 50, 500);
}

/// <summary>
/// Record of API key usage.
/// </summary>
public record ApiKeyUsageRecord
{
    /// <summary>
    /// When the request was made.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// IP address of the requester.
    /// </summary>
    public string IPAddress { get; init; }

    /// <summary>
    /// API endpoint that was called.
    /// </summary>
    public string Endpoint { get; init; }

    /// <summary>
    /// Whether the request was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if request failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    public ApiKeyUsageRecord(
        DateTime timestamp,
        string ipAddress,
        string endpoint,
        bool success,
        string? errorMessage = null)
    {
        Timestamp = timestamp;
        IPAddress = ipAddress ?? string.Empty;
        Endpoint = endpoint ?? string.Empty;
        Success = success;
        ErrorMessage = errorMessage;
    }
}

/// <summary>
/// Usage statistics for an API key.
/// </summary>
public record ApiKeyUsageStats
{
    /// <summary>
    /// Total requests made.
    /// </summary>
    public long TotalRequests { get; init; }

    /// <summary>
    /// Successful requests.
    /// </summary>
    public long SuccessfulRequests { get; init; }

    /// <summary>
    /// Failed requests.
    /// </summary>
    public long FailedRequests { get; init; }

    /// <summary>
    /// Requests this minute.
    /// </summary>
    public int RequestsThisMinute { get; init; }

    /// <summary>
    /// Requests this hour.
    /// </summary>
    public int RequestsThisHour { get; init; }

    /// <summary>
    /// Requests today.
    /// </summary>
    public int RequestsToday { get; init; }

    /// <summary>
    /// Most called endpoint.
    /// </summary>
    public string? MostCalledEndpoint { get; init; }

    /// <summary>
    /// Most common IP address.
    /// </summary>
    public string? MostCommonIP { get; init; }

    /// <summary>
    /// When stats were calculated.
    /// </summary>
    public DateTime CalculatedAt { get; init; }

    public ApiKeyUsageStats(
        long totalRequests,
        long successfulRequests,
        long failedRequests,
        int requestsThisMinute,
        int requestsThisHour,
        int requestsToday,
        string? mostCalledEndpoint = null,
        string? mostCommonIP = null)
    {
        TotalRequests = totalRequests;
        SuccessfulRequests = successfulRequests;
        FailedRequests = failedRequests;
        RequestsThisMinute = requestsThisMinute;
        RequestsThisHour = requestsThisHour;
        RequestsToday = requestsToday;
        MostCalledEndpoint = mostCalledEndpoint;
        MostCommonIP = mostCommonIP;
        CalculatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Calculate success rate.
    /// </summary>
    public decimal GetSuccessRate()
    {
        if (TotalRequests == 0) return 100m;
        return Math.Round((decimal)SuccessfulRequests / TotalRequests * 100, 2);
    }
}

/// <summary>
/// API key display information (safe to show in UI).
/// </summary>
public record ApiKeyDisplayInfo
{
    /// <summary>
    /// Key ID.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Key name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Key prefix for identification.
    /// </summary>
    public string KeyPrefix { get; init; }

    /// <summary>
    /// Masked key for display (e.g., "ti_live_****...****").
    /// </summary>
    public string MaskedKey { get; init; }

    /// <summary>
    /// When the key was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// When the key was last used.
    /// </summary>
    public DateTime? LastUsedAt { get; init; }

    /// <summary>
    /// When the key expires.
    /// </summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>
    /// Current status.
    /// </summary>
    public string Status { get; init; }

    public ApiKeyDisplayInfo(
        Guid id,
        string name,
        string keyPrefix,
        DateTime createdAt,
        DateTime? lastUsedAt,
        DateTime? expiresAt,
        string status)
    {
        Id = id;
        Name = name ?? string.Empty;
        KeyPrefix = keyPrefix ?? string.Empty;
        MaskedKey = $"{keyPrefix}****...****";
        CreatedAt = createdAt;
        LastUsedAt = lastUsedAt;
        ExpiresAt = expiresAt;
        Status = status ?? "Unknown";
    }
}
