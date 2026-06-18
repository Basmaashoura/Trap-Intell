using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Commands.Enums;
using Trap_Intel.Domain.Commands.ValueObjects;

namespace Trap_Intel.Domain.Commands.Policies;

/// <summary>
/// Policy for command validation.
/// Encapsulates all validation logic for command creation and operations.
/// </summary>
public static class CommandValidationPolicy
{
    /// <summary>
    /// Validate command creation parameters.
    /// </summary>
    public static Result ValidateCreate(
        Guid honeypotId,
        Guid organizationId,
        Guid issuedByUserId,
        AgentCommandType commandType,
        CommandPayload? payload)
    {
        if (honeypotId == Guid.Empty)
            return Result.Failure(CommandErrors.InvalidHoneypotId);

        if (organizationId == Guid.Empty)
            return Result.Failure(CommandErrors.InvalidOrganizationId);

        if (issuedByUserId == Guid.Empty)
            return Result.Failure(CommandErrors.InvalidIssuedByUserId);

        if (commandType == AgentCommandType.Unknown)
            return Result.Failure(CommandErrors.InvalidCommandType);

        if (payload == null)
            return Result.Failure(CommandErrors.InvalidPayload);

        return Result.Success();
    }

    /// <summary>
    /// Validate IP address format.
    /// </summary>
    public static Result ValidateIPAddress(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return Result.Failure(
                Error.Custom("AgentCommand.InvalidIPAddress", "IP address cannot be empty"));

        // Basic IPv4 validation
        if (System.Net.IPAddress.TryParse(ipAddress, out _))
            return Result.Success();

        return Result.Failure(
            Error.Custom("AgentCommand.InvalidIPAddress", $"'{ipAddress}' is not a valid IP address"));
    }

    /// <summary>
    /// Validate CIDR range format.
    /// </summary>
    public static Result ValidateCIDRRange(string? cidrRange)
    {
        if (string.IsNullOrWhiteSpace(cidrRange))
            return Result.Failure(
                Error.Custom("AgentCommand.InvalidCIDR", "CIDR range cannot be empty"));

        // Basic CIDR validation (e.g., "192.168.1.0/24")
        var parts = cidrRange.Split('/');
        if (parts.Length != 2)
            return Result.Failure(
                Error.Custom("AgentCommand.InvalidCIDR", "CIDR must be in format IP/prefix (e.g., 192.168.1.0/24)"));

        if (!System.Net.IPAddress.TryParse(parts[0], out _))
            return Result.Failure(
                Error.Custom("AgentCommand.InvalidCIDR", "Invalid IP address in CIDR range"));

        if (!int.TryParse(parts[1], out var prefix) || prefix < 0 || prefix > 32)
            return Result.Failure(
                Error.Custom("AgentCommand.InvalidCIDR", "CIDR prefix must be between 0 and 32"));

        return Result.Success();
    }

    /// <summary>
    /// Validate error message is not empty.
    /// </summary>
    public static Result ValidateErrorMessage(string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            return Result.Failure(CommandErrors.InvalidResult);

        return Result.Success();
    }

    /// <summary>
    /// Validate command result is not null.
    /// </summary>
    public static Result ValidateResult(CommandResult? result)
    {
        if (result == null)
            return Result.Failure(CommandErrors.InvalidResult);

        return Result.Success();
    }

    /// <summary>
    /// Check if command type requires immediate execution.
    /// </summary>
    public static bool RequiresImmediateExecution(AgentCommandType commandType)
    {
        return commandType switch
        {
            AgentCommandType.StopAgent => true,
            AgentCommandType.RestartAgent => true,
            AgentCommandType.BlockIP => true,
            AgentCommandType.BlockIPRange => true,
            _ => false
        };
    }

    /// <summary>
    /// Check if command type is security-related.
    /// </summary>
    public static bool IsSecurityCommand(AgentCommandType commandType)
    {
        return commandType switch
        {
            AgentCommandType.BlockIP => true,
            AgentCommandType.UnblockIP => true,
            AgentCommandType.BlockIPRange => true,
            _ => false
        };
    }

    /// <summary>
    /// Check if command type is agent control.
    /// </summary>
    public static bool IsAgentControlCommand(AgentCommandType commandType)
    {
        return commandType switch
        {
            AgentCommandType.StopAgent => true,
            AgentCommandType.RestartAgent => true,
            AgentCommandType.UpdateAgent => true,
            _ => false
        };
    }

    /// <summary>
    /// Check if command type is configuration-related.
    /// </summary>
    public static bool IsConfigurationCommand(AgentCommandType commandType)
    {
        return commandType switch
        {
            AgentCommandType.UpdateConfiguration => true,
            AgentCommandType.UpdateDeceptionAsset => true,
            _ => false
        };
    }

    /// <summary>
    /// Check if command type is diagnostic/monitoring.
    /// </summary>
    public static bool IsDiagnosticCommand(AgentCommandType commandType)
    {
        return commandType switch
        {
            AgentCommandType.RunDiagnostics => true,
            AgentCommandType.CollectMetrics => true,
            AgentCommandType.FlushLogs => true,
            AgentCommandType.GenerateReport => true,
            _ => false
        };
    }
}
