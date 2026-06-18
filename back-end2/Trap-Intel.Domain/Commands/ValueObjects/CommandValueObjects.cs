using System.Text.Json;
using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Commands.ValueObjects;

/// <summary>
/// Command payload sent to Go honeypot agent.
/// Strongly-typed wrapper around JSON payload.
/// </summary>
public record CommandPayload
{
    public string JsonPayload { get; init; } = "{}";

    // Private constructor for EF Core
    private CommandPayload() { }

    public CommandPayload(string jsonPayload)
    {
        JsonPayload = jsonPayload ?? "{}";
    }

    /// <summary>
    /// Create payload for blocking IP address.
    /// </summary>
    public static CommandPayload ForBlockIP(string ipAddress, string? reason = null)
    {
        var payload = new
        {
            action = "block_ip",
            ip = ipAddress,
            reason = reason ?? "Manual block",
            timestamp = DateTime.UtcNow
        };

        return new CommandPayload(JsonSerializer.Serialize(payload));
    }

    /// <summary>
    /// Create payload for unblocking IP address.
    /// </summary>
    public static CommandPayload ForUnblockIP(string ipAddress, string? reason = null)
    {
        var payload = new
        {
            action = "unblock_ip",
            ip = ipAddress,
            reason = reason ?? "Manual unblock",
            timestamp = DateTime.UtcNow
        };

        return new CommandPayload(JsonSerializer.Serialize(payload));
    }

    /// <summary>
    /// Create payload for blocking IP range (CIDR).
    /// </summary>
    public static CommandPayload ForBlockIPRange(string cidrRange, string? reason = null)
    {
        var payload = new
        {
            action = "block_ip_range",
            cidr = cidrRange,
            reason = reason ?? "Manual range block",
            timestamp = DateTime.UtcNow
        };

        return new CommandPayload(JsonSerializer.Serialize(payload));
    }

    /// <summary>
    /// Create payload for updating configuration.
    /// </summary>
    public static CommandPayload ForUpdateConfiguration(object configuration)
    {
        var payload = new
        {
            action = "update_config",
            config = configuration,
            timestamp = DateTime.UtcNow
        };

        return new CommandPayload(JsonSerializer.Serialize(payload));
    }

    /// <summary>
    /// Create payload for updating deception asset.
    /// </summary>
    public static CommandPayload ForUpdateDeceptionAsset(
        string assetId,
        string assetType,
        string content)
    {
        var payload = new
        {
            action = "update_deception",
            asset_id = assetId,
            asset_type = assetType,
            content = content,
            timestamp = DateTime.UtcNow
        };

        return new CommandPayload(JsonSerializer.Serialize(payload));
    }

    /// <summary>
    /// Create payload for agent restart.
    /// </summary>
    public static CommandPayload ForRestartAgent(bool graceful = true)
    {
        var payload = new
        {
            action = "restart_agent",
            graceful = graceful,
            timestamp = DateTime.UtcNow
        };

        return new CommandPayload(JsonSerializer.Serialize(payload));
    }

    /// <summary>
    /// Create payload for agent stop.
    /// </summary>
    public static CommandPayload ForStopAgent(bool graceful = true)
    {
        var payload = new
        {
            action = "stop_agent",
            graceful = graceful,
            timestamp = DateTime.UtcNow
        };

        return new CommandPayload(JsonSerializer.Serialize(payload));
    }

    /// <summary>
    /// Create payload for agent update.
    /// </summary>
    public static CommandPayload ForUpdateAgent(string targetVersion)
    {
        var payload = new
        {
            action = "update_agent",
            target_version = targetVersion,
            timestamp = DateTime.UtcNow
        };

        return new CommandPayload(JsonSerializer.Serialize(payload));
    }

    /// <summary>
    /// Create payload for running diagnostics.
    /// </summary>
    public static CommandPayload ForRunDiagnostics(bool includeNetworkTests = true, bool includeStorageTests = true)
    {
        var payload = new
        {
            action = "run_diagnostics",
            include_network = includeNetworkTests,
            include_storage = includeStorageTests,
            timestamp = DateTime.UtcNow
        };

        return new CommandPayload(JsonSerializer.Serialize(payload));
    }

    /// <summary>
    /// Create payload for collecting metrics.
    /// </summary>
    public static CommandPayload ForCollectMetrics()
    {
        var payload = new
        {
            action = "collect_metrics",
            timestamp = DateTime.UtcNow
        };

        return new CommandPayload(JsonSerializer.Serialize(payload));
    }

    /// <summary>
    /// Create payload for flushing logs.
    /// </summary>
    public static CommandPayload ForFlushLogs(bool compressLogs = true)
    {
        var payload = new
        {
            action = "flush_logs",
            compress = compressLogs,
            timestamp = DateTime.UtcNow
        };

        return new CommandPayload(JsonSerializer.Serialize(payload));
    }

    /// <summary>
    /// Create payload for generating report.
    /// </summary>
    public static CommandPayload ForGenerateReport(string reportType, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var payload = new
        {
            action = "generate_report",
            report_type = reportType,
            from_date = fromDate,
            to_date = toDate ?? DateTime.UtcNow,
            timestamp = DateTime.UtcNow
        };

        return new CommandPayload(JsonSerializer.Serialize(payload));
    }

    /// <summary>
    /// Create generic payload.
    /// </summary>
    public static CommandPayload FromObject(object payload)
    {
        return new CommandPayload(JsonSerializer.Serialize(payload));
    }

    /// <summary>
    /// Parse payload to object.
    /// </summary>
    public Result<T> Parse<T>()
    {
        try
        {
            var obj = JsonSerializer.Deserialize<T>(JsonPayload);
            if (obj == null)
                return Result.Failure<T>(
                    Error.Custom("CommandPayload.NullDeserialization", "Failed to deserialize payload"));

            return Result.Success(obj);
        }
        catch (JsonException ex)
        {
            return Result.Failure<T>(
                Error.Custom("CommandPayload.InvalidJson", ex.Message));
        }
    }
}

/// <summary>
/// Result returned by Go honeypot agent after command execution.
/// </summary>
public record CommandResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? ResultData { get; init; }
    public DateTime? CompletedAt { get; init; }
    public TimeSpan? ExecutionDuration { get; init; }

    // Private constructor for EF Core
    private CommandResult() { }

    public CommandResult(
        bool success,
        string message,
        string? resultData = null,
        DateTime? completedAt = null,
        TimeSpan? executionDuration = null)
    {
        Success = success;
        Message = message ?? string.Empty;
        ResultData = resultData;
        CompletedAt = completedAt ?? DateTime.UtcNow;
        ExecutionDuration = executionDuration ?? TimeSpan.Zero;
    }

    /// <summary>
    /// Create success result.
    /// </summary>
    public static CommandResult CreateSuccess(
        string message,
        string? resultData = null,
        TimeSpan? executionDuration = null)
    {
        return new CommandResult(
            success: true,
            message: message,
            resultData: resultData,
            executionDuration: executionDuration);
    }

    /// <summary>
    /// Create failure result.
    /// </summary>
    public static CommandResult CreateFailure(
        string message,
        string? errorDetails = null)
    {
        return new CommandResult(
            success: false,
            message: message,
            resultData: errorDetails);
    }

    /// <summary>
    /// Parse result data to object.
    /// </summary>
    public Result<T> ParseResultData<T>()
    {
        try
        {
            if (string.IsNullOrEmpty(ResultData))
                return Result.Failure<T>(
                    Error.Custom("CommandResult.EmptyData", "Result data is empty"));

            var obj = JsonSerializer.Deserialize<T>(ResultData);
            if (obj == null)
                return Result.Failure<T>(
                    Error.Custom("CommandResult.NullDeserialization", "Failed to deserialize result data"));

            return Result.Success(obj);
        }
        catch (JsonException ex)
        {
            return Result.Failure<T>(
                Error.Custom("CommandResult.InvalidJson", ex.Message));
        }
    }
}

/// <summary>
/// Timeout configuration for commands.
/// </summary>
public record CommandTimeout
{
    public TimeSpan Timeout { get; init; }
    public int MaxRetries { get; init; }

    // Private constructor for EF Core
    private CommandTimeout() { }

    public CommandTimeout(TimeSpan timeout, int maxRetries = 3)
    {
        Timeout = timeout;
        MaxRetries = maxRetries > 0 ? maxRetries : 1;
    }

    /// <summary>
    /// Default timeout (30 seconds, 3 retries).
    /// </summary>
    public static CommandTimeout Default => new(TimeSpan.FromSeconds(30), 3);

    /// <summary>
    /// Short timeout for fast operations (10 seconds, 2 retries).
    /// </summary>
    public static CommandTimeout Short => new(TimeSpan.FromSeconds(10), 2);

    /// <summary>
    /// Long timeout for heavy operations (2 minutes, 1 retry).
    /// </summary>
    public static CommandTimeout Long => new(TimeSpan.FromMinutes(2), 1);

    /// <summary>
    /// Critical timeout for urgent operations (5 seconds, 5 retries).
    /// </summary>
    public static CommandTimeout Critical => new(TimeSpan.FromSeconds(5), 5);
}
