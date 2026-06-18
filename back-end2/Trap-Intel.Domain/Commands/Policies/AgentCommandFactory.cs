using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Commands.Enums;
using Trap_Intel.Domain.Commands.Events;
using Trap_Intel.Domain.Commands.ValueObjects;

namespace Trap_Intel.Domain.Commands.Policies;

/// <summary>
/// Factory for creating specialized agent commands.
/// Encapsulates command creation logic with appropriate defaults.
/// </summary>
public static class AgentCommandFactory
{
    /// <summary>
    /// Create command to block an IP address.
    /// </summary>
    public static Result<AgentCommand> CreateBlockIPCommand(
        Guid honeypotId,
        Guid organizationId,
        Guid issuedByUserId,
        string ipAddress,
        string? reason = null)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return Result.Failure<AgentCommand>(
                Error.Custom("AgentCommand.InvalidIPAddress", "IP address cannot be empty"));

        var payload = CommandPayload.ForBlockIP(ipAddress, reason);

        return AgentCommand.Create(
            honeypotId,
            organizationId,
            issuedByUserId,
            AgentCommandType.BlockIP,
            payload,
            CommandPriority.High,
            CommandTimeout.Short);
    }

    /// <summary>
    /// Create command to unblock an IP address.
    /// </summary>
    public static Result<AgentCommand> CreateUnblockIPCommand(
        Guid honeypotId,
        Guid organizationId,
        Guid issuedByUserId,
        string ipAddress,
        string? reason = null)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return Result.Failure<AgentCommand>(
                Error.Custom("AgentCommand.InvalidIPAddress", "IP address cannot be empty"));

        var payload = CommandPayload.ForUnblockIP(ipAddress, reason);

        return AgentCommand.Create(
            honeypotId,
            organizationId,
            issuedByUserId,
            AgentCommandType.UnblockIP,
            payload,
            CommandPriority.Normal,
            CommandTimeout.Short);
    }

    /// <summary>
    /// Create command to block an IP range (CIDR).
    /// </summary>
    public static Result<AgentCommand> CreateBlockIPRangeCommand(
        Guid honeypotId,
        Guid organizationId,
        Guid issuedByUserId,
        string cidrRange,
        string? reason = null)
    {
        if (string.IsNullOrWhiteSpace(cidrRange))
            return Result.Failure<AgentCommand>(
                Error.Custom("AgentCommand.InvalidCIDR", "CIDR range cannot be empty"));

        var payload = CommandPayload.ForBlockIPRange(cidrRange, reason);

        return AgentCommand.Create(
            honeypotId,
            organizationId,
            issuedByUserId,
            AgentCommandType.BlockIPRange,
            payload,
            CommandPriority.High,
            CommandTimeout.Short);
    }

    /// <summary>
    /// Create command to update honeypot configuration.
    /// </summary>
    public static Result<AgentCommand> CreateUpdateConfigCommand(
        Guid honeypotId,
        Guid organizationId,
        Guid issuedByUserId,
        object configuration)
    {
        if (configuration == null)
            return Result.Failure<AgentCommand>(
                Error.Custom("AgentCommand.InvalidConfiguration", "Configuration cannot be null"));

        var payload = CommandPayload.ForUpdateConfiguration(configuration);

        return AgentCommand.Create(
            honeypotId,
            organizationId,
            issuedByUserId,
            AgentCommandType.UpdateConfiguration,
            payload,
            CommandPriority.Normal,
            CommandTimeout.Default);
    }

    /// <summary>
    /// Create command to update a deception asset.
    /// </summary>
    public static Result<AgentCommand> CreateUpdateDeceptionAssetCommand(
        Guid honeypotId,
        Guid organizationId,
        Guid issuedByUserId,
        string assetId,
        string assetType,
        string content)
    {
        if (string.IsNullOrWhiteSpace(assetId))
            return Result.Failure<AgentCommand>(
                Error.Custom("AgentCommand.InvalidAssetId", "Asset ID cannot be empty"));

        if (string.IsNullOrWhiteSpace(assetType))
            return Result.Failure<AgentCommand>(
                Error.Custom("AgentCommand.InvalidAssetType", "Asset type cannot be empty"));

        var payload = CommandPayload.ForUpdateDeceptionAsset(assetId, assetType, content);

        return AgentCommand.Create(
            honeypotId,
            organizationId,
            issuedByUserId,
            AgentCommandType.UpdateDeceptionAsset,
            payload,
            CommandPriority.Normal,
            CommandTimeout.Default);
    }

    /// <summary>
    /// Create command to restart the agent.
    /// </summary>
    public static Result<AgentCommand> CreateRestartAgentCommand(
        Guid honeypotId,
        Guid organizationId,
        Guid issuedByUserId,
        bool graceful = true)
    {
        var payload = CommandPayload.ForRestartAgent(graceful);

        return AgentCommand.Create(
            honeypotId,
            organizationId,
            issuedByUserId,
            AgentCommandType.RestartAgent,
            payload,
            CommandPriority.Critical,
            CommandTimeout.Long);
    }

    /// <summary>
    /// Create command to stop the agent.
    /// </summary>
    public static Result<AgentCommand> CreateStopAgentCommand(
        Guid honeypotId,
        Guid organizationId,
        Guid issuedByUserId,
        bool graceful = true)
    {
        var payload = CommandPayload.ForStopAgent(graceful);

        return AgentCommand.Create(
            honeypotId,
            organizationId,
            issuedByUserId,
            AgentCommandType.StopAgent,
            payload,
            CommandPriority.Critical,
            CommandTimeout.Short);
    }

    /// <summary>
    /// Create command to update the agent software.
    /// </summary>
    public static Result<AgentCommand> CreateUpdateAgentCommand(
        Guid honeypotId,
        Guid organizationId,
        Guid issuedByUserId,
        string targetVersion)
    {
        if (string.IsNullOrWhiteSpace(targetVersion))
            return Result.Failure<AgentCommand>(
                Error.Custom("AgentCommand.InvalidVersion", "Target version cannot be empty"));

        var payload = CommandPayload.ForUpdateAgent(targetVersion);

        return AgentCommand.Create(
            honeypotId,
            organizationId,
            issuedByUserId,
            AgentCommandType.UpdateAgent,
            payload,
            CommandPriority.High,
            CommandTimeout.Long);
    }

    /// <summary>
    /// Create command to run diagnostics.
    /// </summary>
    public static Result<AgentCommand> CreateRunDiagnosticsCommand(
        Guid honeypotId,
        Guid organizationId,
        Guid issuedByUserId,
        bool includeNetworkTests = true,
        bool includeStorageTests = true)
    {
        var payload = CommandPayload.ForRunDiagnostics(includeNetworkTests, includeStorageTests);

        return AgentCommand.Create(
            honeypotId,
            organizationId,
            issuedByUserId,
            AgentCommandType.RunDiagnostics,
            payload,
            CommandPriority.Low,
            CommandTimeout.Long);
    }

    /// <summary>
    /// Create command to collect metrics.
    /// </summary>
    public static Result<AgentCommand> CreateCollectMetricsCommand(
        Guid honeypotId,
        Guid organizationId,
        Guid issuedByUserId)
    {
        var payload = CommandPayload.ForCollectMetrics();

        return AgentCommand.Create(
            honeypotId,
            organizationId,
            issuedByUserId,
            AgentCommandType.CollectMetrics,
            payload,
            CommandPriority.Low,
            CommandTimeout.Short);
    }

    /// <summary>
    /// Create command to flush logs to central storage.
    /// </summary>
    public static Result<AgentCommand> CreateFlushLogsCommand(
        Guid honeypotId,
        Guid organizationId,
        Guid issuedByUserId,
        bool compressLogs = true)
    {
        var payload = CommandPayload.ForFlushLogs(compressLogs);

        return AgentCommand.Create(
            honeypotId,
            organizationId,
            issuedByUserId,
            AgentCommandType.FlushLogs,
            payload,
            CommandPriority.Normal,
            CommandTimeout.Default);
    }

    /// <summary>
    /// Create command to generate a report.
    /// </summary>
    public static Result<AgentCommand> CreateGenerateReportCommand(
        Guid honeypotId,
        Guid organizationId,
        Guid issuedByUserId,
        string reportType,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        if (string.IsNullOrWhiteSpace(reportType))
            return Result.Failure<AgentCommand>(
                Error.Custom("AgentCommand.InvalidReportType", "Report type cannot be empty"));

        var payload = CommandPayload.ForGenerateReport(reportType, fromDate, toDate);

        return AgentCommand.Create(
            honeypotId,
            organizationId,
            issuedByUserId,
            AgentCommandType.GenerateReport,
            payload,
            CommandPriority.Low,
            CommandTimeout.Long);
    }

    /// <summary>
    /// Get recommended command settings for a command type.
    /// </summary>
    public static (CommandPriority Priority, CommandTimeout Timeout) GetRecommendedSettings(
        AgentCommandType commandType)
    {
        return commandType switch
        {
            // Critical - immediate response needed
            AgentCommandType.StopAgent => (CommandPriority.Critical, CommandTimeout.Short),
            AgentCommandType.RestartAgent => (CommandPriority.Critical, CommandTimeout.Long),

            // High priority - security operations
            AgentCommandType.BlockIP => (CommandPriority.High, CommandTimeout.Short),
            AgentCommandType.BlockIPRange => (CommandPriority.High, CommandTimeout.Short),
            AgentCommandType.UpdateAgent => (CommandPriority.High, CommandTimeout.Long),

            // Normal priority - configuration
            AgentCommandType.UnblockIP => (CommandPriority.Normal, CommandTimeout.Short),
            AgentCommandType.UpdateConfiguration => (CommandPriority.Normal, CommandTimeout.Default),
            AgentCommandType.UpdateDeceptionAsset => (CommandPriority.Normal, CommandTimeout.Default),
            AgentCommandType.FlushLogs => (CommandPriority.Normal, CommandTimeout.Default),

            // Low priority - background tasks
            AgentCommandType.RunDiagnostics => (CommandPriority.Low, CommandTimeout.Long),
            AgentCommandType.CollectMetrics => (CommandPriority.Low, CommandTimeout.Short),
            AgentCommandType.GenerateReport => (CommandPriority.Low, CommandTimeout.Long),

            // Default
            _ => (CommandPriority.Normal, CommandTimeout.Default)
        };
    }
}
