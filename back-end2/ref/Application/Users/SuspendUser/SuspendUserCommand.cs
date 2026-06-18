using Application.Abstractions.Messaging;

namespace Application.Users.SuspendUser;

/// <summary>
/// Command to suspend a user temporarily.
/// </summary>
/// <param name="UserId">The ID of the user to suspend.</param>
/// <param name="Reason">Optional reason for suspension.</param>
public sealed record SuspendUserCommand(
    Guid UserId,
    string? Reason = null) : ICommand;
