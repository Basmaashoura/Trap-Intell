using Application.Abstractions.Messaging;

namespace Application.Users.Disable2FA;

/// <summary>
/// Command to disable two-factor authentication for a user.
/// </summary>
/// <param name="UserId">The ID of the user disabling 2FA.</param>
/// <param name="Code">The 6-digit 2FA code from authenticator app for verification.</param>
public sealed record Disable2FACommand(
    Guid UserId,
    string Code) : ICommand;
