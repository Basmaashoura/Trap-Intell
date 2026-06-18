using Trap_Intel.Domain.Abstractions;

namespace Trap_Intel.Domain.Commands;

public static class CommandErrors
{
    public static readonly Error InvalidCommandType = Error.Custom(
        "AgentCommand.InvalidCommandType",
        "Invalid command type specified");

    public static readonly Error InvalidHoneypotId = Error.Custom(
        "AgentCommand.InvalidHoneypotId",
        "Honeypot ID cannot be empty");

    public static readonly Error InvalidOrganizationId = Error.Custom(
        "AgentCommand.InvalidOrganizationId",
        "Organization ID cannot be empty");

    public static readonly Error InvalidIssuedByUserId = Error.Custom(
        "AgentCommand.InvalidIssuedByUserId",
        "Issued by user ID cannot be empty");

    public static readonly Error InvalidPayload = Error.Custom(
        "AgentCommand.InvalidPayload",
        "Command payload cannot be null or empty");

    public static readonly Error AlreadySent = Error.Custom(
        "AgentCommand.AlreadySent",
        "Command has already been sent");

    public static readonly Error NotSent = Error.Custom(
        "AgentCommand.NotSent",
        "Command has not been sent yet");

    public static readonly Error AlreadyCompleted = Error.Custom(
        "AgentCommand.AlreadyCompleted",
        "Command has already been completed");

    public static readonly Error AlreadyFailed = Error.Custom(
        "AgentCommand.AlreadyFailed",
        "Command has already failed");

    public static readonly Error AlreadyCancelled = Error.Custom(
        "AgentCommand.AlreadyCancelled",
        "Command has already been cancelled");

    public static readonly Error CannotCancelCompleted = Error.Custom(
        "AgentCommand.CannotCancelCompleted",
        "Cannot cancel a completed command");

    public static readonly Error CannotRetryCompleted = Error.Custom(
        "AgentCommand.CannotRetryCompleted",
        "Cannot retry a completed command");

    public static readonly Error MaxRetriesExceeded = Error.Custom(
        "AgentCommand.MaxRetriesExceeded",
        "Maximum retry attempts exceeded");

    public static readonly Error InvalidTimeout = Error.Custom(
        "AgentCommand.InvalidTimeout",
        "Timeout must be greater than zero");

    public static readonly Error InvalidResult = Error.Custom(
        "AgentCommand.InvalidResult",
        "Command result cannot be null");

    public static readonly Error NotFound = Error.Custom(
        "AgentCommand.NotFound",
        "Command not found");
}
